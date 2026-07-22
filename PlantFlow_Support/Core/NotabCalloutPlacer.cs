using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.ProcessPower.PnP3dObjects;

#nullable disable
namespace PlantFlow_Support
{
  // 페이퍼공간 콜아웃 전용: 디테일마다 새 인스턴스를 만들어 문자·리더 충돌 상태를 공유한다.
  internal sealed class NotabCalloutPlacer
  {
    public enum RequiredSide { Left, Right }
    private enum LeaderCheckScope { All, PlacedCalloutsOnly, None }
    private sealed class Obstacle { public Extents3d Box; public string Owner; }
    private readonly System.Collections.Generic.List<Obstacle> _obstacles = new System.Collections.Generic.List<Obstacle>();
    private readonly System.Collections.Generic.List<Extents3d> _placedBoxes = new System.Collections.Generic.List<Extents3d>();
    private readonly System.Collections.Generic.List<Point3d[]> _placedLeaders = new System.Collections.Generic.List<Point3d[]>();
    private readonly double _minX, _minY, _maxX, _maxY;
    private Extents3d _costRefExt;
    // 최근접 모서리 법선을 기준으로 대각 배치를 먼저 시도한다.
    private static readonly int[] Fan = new int[] { 45, -45, 30, -30, 60, -60, 15, -15, 75, -75, 0, 90, -90, 105, -105, 120, -120, 135, -135, 150, -150, 165, -165, 180 };

    public NotabCalloutPlacer(double minX, double minY, double maxX, double maxY)
    { _minX = minX; _minY = minY; _maxX = maxX; _maxY = maxY; }

    public void AddObstacle(Extents3d obstacle, string owner = "")
    {
      // 장애물 패딩과 콜아웃 간 여백은 별개다. 이름을 공유하면 여백을 키울 때
      // 모든 장애물이 함께 부풀어 자유 공간이 사라진다(cycle95 실측).
      double pad = System.Math.Max(0.0, System.Math.Min(50.0, ReadEnvDouble("PFS_NOTAB_OBSTACLE_PAD", 2.0)));
      _obstacles.Add(new Obstacle { Owner = owner ?? string.Empty, Box = new Extents3d(
        new Point3d(obstacle.MinPoint.X - pad, obstacle.MinPoint.Y - pad, 0.0),
        new Point3d(obstacle.MaxPoint.X + pad, obstacle.MaxPoint.Y + pad, 0.0)) });
    }

    // 콜아웃이 서포트와 앵커에서 얼마나 멀리 퍼지는지를 같은 기준으로 점수화한다.
    public void SetCostReference(Extents3d supportExt, Point3d anchor)
    {
      _costRefExt = new Extents3d(
        new Point3d(System.Math.Min(supportExt.MinPoint.X, anchor.X), System.Math.Min(supportExt.MinPoint.Y, anchor.Y), 0.0),
        new Point3d(System.Math.Max(supportExt.MaxPoint.X, anchor.X), System.Math.Max(supportExt.MaxPoint.Y, anchor.Y), 0.0));
    }

    // tailLength > 0 이면 꺾임 1회 리더(경사 + 수평 꼬리)로 배치한다.
    // p1=꺾임점(elbow), p2=문자 접속점. 0이면 p1=p2=문자 접속점인 직선 1개다.
    public bool TryPlace(Point3d leaderFrom, RequiredSide requiredSide, double width, double height, double gap, double minDx,
      string ownerTag, bool preferDown, double tailLength, out Point3d textCenter, out Point3d p1, out Point3d p2, out bool textLeftOfAnchor, out string diagnostic)
    {
      double startRadius = System.Math.Max(15.0, System.Math.Max(width, height) / 2.0 + gap);
      double maxRadius = System.Math.Max(startRadius, System.Math.Sqrt(System.Math.Pow(System.Math.Max(leaderFrom.X - _minX, _maxX - leaderFrom.X), 2.0) + System.Math.Pow(System.Math.Max(leaderFrom.Y - _minY, _maxY - leaderFrom.Y), 2.0)) + width + height);
      // 루프가 한 번도 돌지 않는 경우까지 컴파일러가 out 할당을 증명할 수 있도록 루프 밖에 둔다.
      string failDiag = "FAIL";
      for (int tier = 0; tier < 3; tier++)
      {
        LeaderCheckScope leaderScope = tier == 0 ? LeaderCheckScope.All : tier == 1 ? LeaderCheckScope.PlacedCalloutsOnly : LeaderCheckScope.None;
        bool found = false;
        double bestCost = double.MaxValue;
        Point3d bestCenter = Point3d.Origin, bestP1 = Point3d.Origin, bestP2 = Point3d.Origin;
        bool bestLeft = false;
        string bestDiag = "FAIL";
        int scanned = 0, rejectOob = 0, rejectBox = 0, rejectExtLeader = 0, rejectCalloutLeader = 0;
        System.Collections.Generic.Dictionary<string, int> extLeaderByOwner = new System.Collections.Generic.Dictionary<string, int>();
        double sign = requiredSide == RequiredSide.Left ? -1.0 : 1.0;
        for (double radius = startRadius; radius <= maxRadius; radius += 2.5)
        {
          foreach (int fanOffset in Fan)
          {
            double angle = fanOffset * System.Math.PI / 180.0;
            double horizontal = System.Math.Max(minDx, radius * System.Math.Abs(System.Math.Cos(angle)));
            double x = leaderFrom.X + sign * horizontal;
            double y = leaderFrom.Y + radius * System.Math.Sin(angle);
            bool textLeft = requiredSide == RequiredSide.Left;
            double boxLeft = textLeft ? x - width : x;
            Extents3d box = new Extents3d(new Point3d(boxLeft, y - height / 2.0, 0.0), new Point3d(boxLeft + width, y + height / 2.0, 0.0));
            if (OutOfBounds(box)) { rejectOob++; continue; }
            // 문자 상자의 리더 접속점은 좌·우 변의 중단이다. 검사, 작도, 등록이 같은 점을 쓴다.
            Point3d candTextEdge = new Point3d(textLeft ? box.MaxPoint.X : box.MinPoint.X, y, 0.0);
            // 꺾임 리더는 문자 접속점에서 앵커 쪽으로 tailLength만큼 물러난 곳이 꺾임점이다.
            // 그 결과 마지막 구간이 수평 꼬리가 된다. 직선이면 두 점이 같다.
            Point3d candElbow = tailLength > 1e-9
              ? new Point3d(textLeft ? candTextEdge.X + tailLength : candTextEdge.X - tailLength, y, 0.0)
              : candTextEdge;
            string reject;
            if (!Free(box, leaderFrom, candElbow, candTextEdge, leaderScope, ownerTag, out reject))
            {
              if (reject == "box") rejectBox++;
              else if (reject.StartsWith("extLeader", System.StringComparison.Ordinal))
              {
                rejectExtLeader++;
                string owner = reject.Length > 10 ? reject.Substring(10) : "unnamed";
                int prev;
                extLeaderByOwner.TryGetValue(owner, out prev);
                extLeaderByOwner[owner] = prev + 1;
              }
              else if (reject == "calloutLeader") rejectCalloutLeader++;
              continue;
            }
            scanned++;
            double angleW = System.Math.Max(0.0, ReadEnvDouble("PFS_NOTAB_CALLOUT_ANGLE_W", 0.0));
            double cost = System.Math.Max(0.0, box.MaxPoint.X - _costRefExt.MaxPoint.X)
              + System.Math.Max(0.0, _costRefExt.MinPoint.X - box.MinPoint.X)
              + System.Math.Max(0.0, box.MaxPoint.Y - _costRefExt.MaxPoint.Y)
              + System.Math.Max(0.0, _costRefExt.MinPoint.Y - box.MinPoint.Y)
              + radius * 0.01
              + System.Math.Abs(fanOffset) * angleW
              // 하단 선호: 앵커보다 위로 올라간 만큼만 가산한다. 제약이 아니라 편향이므로
              // 아래가 막히면 위로 올라가는 탐색은 그대로 살아 있다.
              + ((preferDown && y > leaderFrom.Y) ? (y - leaderFrom.Y) * ReadEnvDouble("PFS_NOTAB_CALLOUT_DOWN_W", 1.0) : 0.0);
            if (cost < bestCost)
            {
              found = true;
              bestCost = cost;
              bestCenter = candTextEdge;
              bestLeft = textLeft;
              bestP1 = candElbow;
              bestP2 = candTextEdge;
              bestDiag = "tier=" + tier + " r=" + radius.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " fan=" + fanOffset + " angleW=" + angleW.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " cost=" + cost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            }
          }
        }
        if (found)
        {
          textCenter = bestCenter;
          p1 = bestP1;
          p2 = bestP2;
          textLeftOfAnchor = bestLeft;
          System.Text.StringBuilder byOwner = new System.Text.StringBuilder();
          foreach (System.Collections.Generic.KeyValuePair<string, int> kv in extLeaderByOwner)
          { if (byOwner.Length > 0) byOwner.Append(","); byOwner.Append(kv.Key).Append("=").Append(kv.Value); }
          diagnostic = bestDiag + " scanned=" + scanned + " obst=" + _obstacles.Count + " reject(oob/box/extLeader/calloutLeader)=" + rejectOob + "/" + rejectBox + "/" + rejectExtLeader + "/" + rejectCalloutLeader
            + " extLeaderBy{" + byOwner.ToString() + "}";
          return true;
        }
        if (tier == 2)
          failDiag = "FAIL tier=2 scanned=" + scanned + " reject(oob/box/extLeader/calloutLeader)=" + rejectOob + "/" + rejectBox + "/" + rejectExtLeader + "/" + rejectCalloutLeader;
      }
      textCenter = Point3d.Origin; p1 = Point3d.Origin; p2 = Point3d.Origin; textLeftOfAnchor = requiredSide == RequiredSide.Left;
      diagnostic = failDiag;
      return false;
    }

    public void Commit(Point3d leaderFrom, Point3d leaderTo, Point3d textCenter, double width, double height)
    {
      bool textLeft = textCenter.X < leaderFrom.X;
      _placedBoxes.Add(textLeft
        ? new Extents3d(new Point3d(textCenter.X - width, textCenter.Y - height / 2.0, 0.0), new Point3d(textCenter.X, textCenter.Y + height / 2.0, 0.0))
        : new Extents3d(new Point3d(textCenter.X, textCenter.Y - height / 2.0, 0.0), new Point3d(textCenter.X + width, textCenter.Y + height / 2.0, 0.0)));
      _placedLeaders.Add(new Point3d[] { leaderFrom, leaderTo });
    }

    // 기존 MLeader 경로 호환용. 직접 콜아웃은 위 단일 선분 overload를 사용한다.
    public void Commit(Point3d anchor, Point3d p1, Point3d p2, Point3d textCenter, double width, double height)
    {
      Commit(anchor, p1, textCenter, width, height);
      if (p1.DistanceTo(p2) > 1e-9)
        _placedLeaders.Add(new Point3d[] { p1, p2 });
    }

    // 밸룬은 원이라 Commit의 "textCenter.X < anchor.X" 좌우 추론이 성립하지 않는다.
    // 외접 사각형과 리더 선분을 명시적으로 등록한다.
    // 리더 검사를 면제할 장애물 소유자. 밸룬 배치 구간에서만 켜고 끈다.
    private string _leaderExemptOwner = string.Empty;
    public void SetLeaderExemptOwner(string owner) { _leaderExemptOwner = owner ?? string.Empty; }

    // 밸룬은 전역 비용 탐색이 아니라 둘레 배치로 자리를 정한다(cycle96).
    // 배치기는 "이 자리가 비었는가"만 판정한다.
    public bool WithinBounds(Extents3d box) { return !OutOfBounds(box); }

    // 상자 겹침은 모든 장애물·기존 콜아웃에 대해 금지한다.
    // 리더 교차는 기존 콜아웃(문자·리더)만 본다 — 밸룬 리더가 서포트·치수 위를 지나는 것은 정상이다.
    // 면제는 호출부가 명시할 때만 적용한다. 기본 경로는 기존의 모든 충돌 검사를 보존한다.
    public bool IsBalloonFree(Extents3d box, Point3d leaderFrom, Point3d leaderTo, out string reject,
      bool exemptSupportBox = false, bool allowCalloutLeaderCrossing = false, bool exemptVerticalMemberBox = false)
    {
      foreach (Obstacle obstacle in _obstacles)
      {
        if (exemptSupportBox && string.Equals(obstacle.Owner ?? string.Empty, "support", System.StringComparison.Ordinal))
          continue;
        if (exemptVerticalMemberBox && string.Equals(obstacle.Owner ?? string.Empty, "vertical-member", System.StringComparison.Ordinal))
          continue;
        if (BoxOverlap(box, obstacle.Box))
        { reject = "box:" + (string.IsNullOrEmpty(obstacle.Owner) ? "unnamed" : obstacle.Owner); return false; }
      }
      foreach (Extents3d placed in _placedBoxes)
      {
        if (BoxOverlap(box, placed)) { reject = "box:callout"; return false; }
        if (!allowCalloutLeaderCrossing && SegIntersectsBox(leaderFrom, leaderTo, placed)) { reject = "leader:calloutBox"; return false; }
      }
      foreach (Point3d[] leader in _placedLeaders)
      {
        if (!allowCalloutLeaderCrossing && (SegIntersectsBox(leader[0], leader[1], box) || SegsIntersect(leaderFrom, leaderTo, leader[0], leader[1])))
        { reject = "leader:calloutLeader"; return false; }
      }
      reject = string.Empty;
      return true;
    }

    // "한적한 자리"를 수치로 바꾼다 = 이 상자에서 가장 가까운 장애물·기존 콜아웃까지의 여유.
    // 겹치면 0. 클수록 한적하다.
    public double ClearanceTo(Extents3d box, bool exemptSupportBox = false, bool exemptVerticalMemberBox = false)
    {
      double best = double.MaxValue;
      foreach (Obstacle obstacle in _obstacles)
      {
        if (exemptSupportBox && string.Equals(obstacle.Owner ?? string.Empty, "support", System.StringComparison.Ordinal))
          continue;
        if (exemptVerticalMemberBox && string.Equals(obstacle.Owner ?? string.Empty, "vertical-member", System.StringComparison.Ordinal))
          continue;
        best = System.Math.Min(best, BoxGap(box, obstacle.Box));
      }
      foreach (Extents3d placed in _placedBoxes)
        best = System.Math.Min(best, BoxGap(box, placed));
      return best == double.MaxValue ? 1000.0 : best;
    }

    private static double BoxGap(Extents3d a, Extents3d b)
    {
      double dx = System.Math.Max(0.0, System.Math.Max(b.MinPoint.X - a.MaxPoint.X, a.MinPoint.X - b.MaxPoint.X));
      double dy = System.Math.Max(0.0, System.Math.Max(b.MinPoint.Y - a.MaxPoint.Y, a.MinPoint.Y - b.MaxPoint.Y));
      return System.Math.Sqrt(dx * dx + dy * dy);
    }

    public void CommitBalloonBox(Point3d anchor, Point3d touch, Extents3d box)
    {
      _placedBoxes.Add(box);
      // 리더는 앵커→원주 직선 1개다(꺾임 없음). 원 안 구간은 등록하지 않는다.
      _placedLeaders.Add(new Point3d[] { anchor, touch });
    }

    private bool OutOfBounds(Extents3d box)
    { return box.MinPoint.X < _minX || box.MaxPoint.X > _maxX || box.MinPoint.Y < _minY || box.MaxPoint.Y > _maxY; }

    private bool Free(Extents3d box, Point3d anchor, Point3d p1, Point3d p2, LeaderCheckScope leaderScope, string ownerTag, out string reject)
    {
      foreach (Obstacle obstacle in _obstacles)
      {
        if (!string.IsNullOrEmpty(obstacle.Owner)
          && string.Equals(ownerTag ?? string.Empty, obstacle.Owner, System.StringComparison.Ordinal)) continue;
        if (BoxOverlap(box, obstacle.Box)) { reject = "box"; return false; }
        // 밸룬은 부재를 가리키므로 리더가 서포트 위를 지나는 것이 정상이다.
        // 이 장애물의 리더 검사만 면제한다(문자 상자 겹침 검사는 유지).
        if (!string.IsNullOrEmpty(_leaderExemptOwner)
          && string.Equals(obstacle.Owner ?? string.Empty, _leaderExemptOwner, System.StringComparison.Ordinal)) continue;
        // 장애물 리더 검사는 tier 규칙을 따른다. 기둥은 문자 상자만 계속 차단한다.
        if (leaderScope == LeaderCheckScope.All
          && (SegIntersectsBox(anchor, p1, obstacle.Box) || SegIntersectsBox(p1, p2, obstacle.Box)))
        { reject = "extLeader|" + (string.IsNullOrEmpty(obstacle.Owner) ? "unnamed" : obstacle.Owner); return false; }
      }
      // 콜아웃끼리는 겹치지만 않으면 되는 게 아니라 읽을 여백이 필요하다.
      // 겹침 0인데 간격 3이면 육안으로는 붙어 보인다(RC1 L-65×65×6 ↔ UB-003 실측).
      double calloutPad = System.Math.Max(0.0, ReadEnvDouble("PFS_NOTAB_CALLOUT_PAD", 8.0));
      foreach (Extents3d placed in _placedBoxes)
      {
        Extents3d padded = calloutPad <= 0.0 ? placed : new Extents3d(
          new Point3d(placed.MinPoint.X - calloutPad, placed.MinPoint.Y - calloutPad, 0.0),
          new Point3d(placed.MaxPoint.X + calloutPad, placed.MaxPoint.Y + calloutPad, 0.0));
        if (BoxOverlap(box, padded)) { reject = "box"; return false; }
        if (leaderScope != LeaderCheckScope.None && (SegIntersectsBox(anchor, p1, placed) || SegIntersectsBox(p1, p2, placed))) { reject = "calloutLeader"; return false; }
      }
      if (leaderScope != LeaderCheckScope.None)
      {
        foreach (Point3d[] leader in _placedLeaders)
        {
          if (SegIntersectsBox(leader[0], leader[1], box) || SegsIntersect(anchor, p1, leader[0], leader[1]) || SegsIntersect(p1, p2, leader[0], leader[1])) { reject = "calloutLeader"; return false; }
        }
      }
      reject = string.Empty;
      return true;
    }

    private static bool BoxOverlap(Extents3d a, Extents3d b)
    { return a.MaxPoint.X >= b.MinPoint.X && a.MinPoint.X <= b.MaxPoint.X && a.MaxPoint.Y >= b.MinPoint.Y && a.MinPoint.Y <= b.MaxPoint.Y; }

    private static bool PointInBox(Point3d point, Extents3d box)
    { return point.X >= box.MinPoint.X && point.X <= box.MaxPoint.X && point.Y >= box.MinPoint.Y && point.Y <= box.MaxPoint.Y; }

    private static bool SegIntersectsBox(Point3d start, Point3d end, Extents3d box)
    {
      if (PointInBox(start, box)) return false;
      if (PointInBox(end, box)) return true;
      Point3d bl = box.MinPoint, tr = box.MaxPoint;
      Point3d br = new Point3d(tr.X, bl.Y, 0.0), tl = new Point3d(bl.X, tr.Y, 0.0);
      return SegsIntersect(start, end, bl, br) || SegsIntersect(start, end, br, tr) || SegsIntersect(start, end, tr, tl) || SegsIntersect(start, end, tl, bl);
    }

    private static bool SegsIntersect(Point3d a, Point3d b, Point3d c, Point3d d)
    {
      double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);
      if (System.Math.Abs(denominator) < 1e-9) return false;
      double r = ((a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y)) / denominator;
      double s = ((a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y)) / denominator;
      // 같은 후보의 두 세그먼트는 이 함수를 서로 비교하지 않는다. 그 밖의 끝점 접촉은 충돌로 처리한다.
      return r >= 0.0 && r <= 1.0 && s >= 0.0 && s <= 1.0;
    }

    private static double ReadEnvDouble(string name, double fallback)
    {
      double value;
      string raw = System.Environment.GetEnvironmentVariable(name);
      return !string.IsNullOrWhiteSpace(raw) && double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    // 밸룬 화살표를 부재 외곽에 닿게 하려면 밖에서도 필요하다.
    public static Point3d BoxEdgeToward(Extents3d box, Point3d anchor)
    {
      double x = (box.MinPoint.X + box.MaxPoint.X) / 2.0, y = (box.MinPoint.Y + box.MaxPoint.Y) / 2.0;
      double dx = anchor.X - x, dy = anchor.Y - y;
      if (System.Math.Abs(dx) < 1e-9 && System.Math.Abs(dy) < 1e-9) return new Point3d(x, y, 0.0);
      double sx = System.Math.Abs(dx) > 1e-9 ? (box.MaxPoint.X - box.MinPoint.X) / 2.0 / System.Math.Abs(dx) : double.PositiveInfinity;
      double sy = System.Math.Abs(dy) > 1e-9 ? (box.MaxPoint.Y - box.MinPoint.Y) / 2.0 / System.Math.Abs(dy) : double.PositiveInfinity;
      double scale = System.Math.Min(sx, sy);
      return new Point3d(x + dx * scale, y + dy * scale, 0.0);
    }

    // U-bolt는 U자 하변 중앙이 빈 공간이므로 좌우 세로 변 중점만 사용한다.
    // 중심선과 동률(dx == 0)이면 우변을 택해 결과를 결정적으로 유지한다.
    public static Point3d BoxVerticalEdgeMidpointToward(Extents3d box, Point3d target)
    {
      double minX = box.MinPoint.X, maxX = box.MaxPoint.X;
      double cy = (box.MinPoint.Y + box.MaxPoint.Y) / 2.0;
      double cx = (minX + maxX) / 2.0;
      return new Point3d(target.X >= cx ? maxX : minX, cy, 0.0);
    }
  }
}
