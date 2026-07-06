## RS6 (계측본 — Phase 0 PRIMITIV 관측용 임시)
## 사용법:
##   1) 배포된 RS6.py 백업:  D:\...\CustomScripts\Support\RS6\RS6.py  ->  RS6.py.bak
##   2) 이 파일 내용을 그 RS6.py 에 덮어쓰기(파일명은 반드시 RS6.py 유지)
##   3) (먼저) 변형 19404 ContentGeometryTemplate 을 PRIMITIV_PROBE -> RS6 으로 원복(.acat/.pspc)
##   4) PLANTREGISTERCUSTOMSCRIPTS -> 15A 새로 배치
##   5) 큰 박스(300^3)만 나오면 성공 -> 바탕화면/TEMP/C:\Temp 의 primitiv_probe.txt 회수
##   6) 끝나면 RS6.py.bak 으로 원복
##--------------------------------
from aqa.math import *
from varmain.primitiv import *
from varmain.var_basic import *
from varmain.custom import *

import os
import sys
directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)

from support_data import pipe_size_JIS as pipe_size, profile

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *


# ===== PRIMITIV 관측 헬퍼 (임시) ============================================
def _probe_paths():
  ps = []
  try:
    ps.append(os.path.join(os.path.expanduser("~"), "Desktop", "primitiv_probe.txt"))
  except Exception:
    pass
  try:
    t = os.environ.get("TEMP") or os.environ.get("TMP")
    if t:
      ps.append(os.path.join(t, "primitiv_probe.txt"))
  except Exception:
    pass
  ps.append(r"C:\Temp\primitiv_probe.txt")
  ps.append("primitiv_probe.txt")
  return ps


def _probe_write(text):
  for p in _probe_paths():
    try:
      f = open(p, "w")
      try:
        f.write(text)
      finally:
        f.close()
      return "PY:" + str(p)
    except Exception:
      continue
  try:
    from System.IO import File
    for p in _probe_paths():
      try:
        File.WriteAllText(p, text)
        return "DOTNET:" + str(p)
      except Exception:
        continue
  except Exception:
    pass
  try:
    print(text)
  except Exception:
    pass
  return "PRINT_ONLY"


def _probe_rep(v):
  try:
    r = repr(v)
  except Exception as e:
    return "REPR_ERR:" + str(e)
  return r[:300] + "...(trunc)" if len(r) > 300 else r


def _probe_dump(obj):
  lines = []
  try:
    lines.append("  type = " + str(type(obj)))
  except Exception as e:
    lines.append("  type = ERR:" + str(e))
  try:
    names = dir(obj)
  except Exception as e:
    names = []
    lines.append("  dir() ERR:" + str(e))
  for a in names:
    if a.startswith("__"):
      continue
    try:
      val = getattr(obj, a)
    except Exception as e:
      lines.append("  ." + a + " = GET_ERR:" + str(e))
      continue
    lines.append("  ." + a + "()  [callable]" if callable(val)
                 else "  ." + a + " = " + _probe_rep(val))
  for cand in ("extents", "Extents", "bbox", "BoundingBox", "GeometricExtents",
               "min", "max", "Min", "Max", "center", "Center", "origin",
               "Origin", "solid", "Solid", "id", "Id", "ObjectId", "handle", "Handle"):
    try:
      if hasattr(obj, cand):
        lines.append("  >> " + cand + " = " + _probe_rep(getattr(obj, cand)))
    except Exception:
      pass
  return "\n".join(lines)


def _probe_prim(report, name, fn, save_basename=None):
  report.append("=" * 60)
  report.append("PRIMITIVE: " + name)
  try:
    obj = fn()
    _probe_values(report, obj, save_basename)
    try:
      obj.erase()
    except Exception as e:
      report.append("  (erase failed: " + str(e) + ")")
  except Exception as e:
    report.append("  CONSTRUCT_ERR: " + str(e))


def _probe_call(obj, method, *args):
  """메서드를 호출해 결과 repr 반환(가드). 인자 0개부터 시도."""
  try:
    m = getattr(obj, method, None)
    if m is None:
      return method + " : (없음)"
    r = m(*args)
    return method + "(" + ",".join(repr(a) for a in args) + ") = " + _probe_rep(r)
  except Exception as e:
    return method + "(" + ",".join(repr(a) for a in args) + ") ERR: " + str(e)


def _probe_values(report, obj, save_basename=None):
  """프리미티브의 값-반환 메서드 호출: extents/행렬/파라미터/정점/메시export."""
  report.append(_probe_call(obj, "extents"))
  report.append(_probe_call(obj, "transformationMatrix"))
  report.append(_probe_call(obj, "parameters"))
  npts_line = _probe_call(obj, "numberOfPoints")
  report.append(npts_line)
  # 정점 일부
  for i in range(0, 12):
    try:
      pa = getattr(obj, "pointAt", None)
      if pa is None:
        break
      report.append("  pointAt(" + str(i) + ") = " + _probe_rep(pa(i)))
    except Exception as e:
      report.append("  pointAt(" + str(i) + ") STOP: " + str(e))
      break
  # 메시 export 시도(여러 확장자) — 성공하면 포팅 불요 경로 확보
  if save_basename:
    try:
      _savedir = os.path.join(os.path.expanduser("~"), "Desktop")
    except Exception:
      _savedir = r"C:\Temp"
    for ext in (".stl", ".obj", ".ply", ".dae", ".sat", ".json"):
      path = os.path.join(_savedir, save_basename + ext)
      try:
        sm = getattr(obj, "saveMeshAs", None)
        if sm is None:
          report.append("  saveMeshAs: (메서드 없음)")
          break
        res = sm(path)
        report.append("  saveMeshAs('" + path + "') = " + _probe_rep(res)
                      + "  [파일생성?: " + str(os.path.exists(path)) + "]")
      except Exception as e:
        report.append("  saveMeshAs(" + ext + ") ERR: " + str(e))


def _probe_run(s):
  report = ["PRIMITIV_PROBE report v2 (via RS6 instrumented)"]
  try:
    report.append("python.version = " + str(getattr(sys, "version", "?")))
  except Exception as e:
    report.append("sys ERR: " + str(e))

  report.append("=" * 60)
  report.append("MODULE varmain.primitiv 심볼:")
  try:
    import varmain.primitiv as P
    for a in dir(P):
      if a.startswith("__"):
        continue
      try:
        v = getattr(P, a)
        report.append("  " + a + " : " + ("callable" if callable(v) else _probe_rep(v)))
      except Exception as e:
        report.append("  " + a + " : ERR " + str(e))
    try:
      import inspect
      for fname in ("BOX", "CYLINDER", "TORUS", "SPHERE"):
        fn = getattr(P, fname, None)
        if fn is None:
          continue
        try:
          try:
            sig = str(inspect.signature(fn))
          except Exception:
            sig = str(inspect.getargspec(fn))
          report.append("  signature " + fname + " " + sig)
        except Exception as e:
          report.append("  signature " + fname + " ERR " + str(e))
    except Exception as e:
      report.append("  inspect 불가: " + str(e))
  except Exception as e:
    report.append("  import ERR: " + str(e))

  _probe_prim(report, "BOX(L=100,W=200,H=50)", lambda: BOX(s, L=100.0, W=200.0, H=50.0), "probe_box")
  _probe_prim(report, "CYLINDER(R=50,H=300,O=0)", lambda: CYLINDER(s, R=50.0, H=300.0, O=0.0), "probe_cyl")
  _probe_prim(report, "CYLINDER(R1=60,R2=30,H=300,O=0)", lambda: CYLINDER(s, R1=60.0, R2=30.0, H=300.0, O=0.0))
  _probe_prim(report, "TORUS(R1=100,R2=20)", lambda: TORUS(s, R1=100.0, R2=20.0))
  _probe_prim(report, "SPHERE(R=80)", lambda: SPHERE(s, R=80.0))

  report.append("=" * 60)
  report.append("TRANSFORM: BOX(100,100,100): 생성 -> translate((10,20,30)) -> rotateZ(90) -> rotateX(90)")
  try:
    b = BOX(s, L=100.0, W=100.0, H=100.0)
    report.append("[create]")
    report.append(_probe_call(b, "extents")); report.append(_probe_call(b, "transformationMatrix"))
    b.translate((10.0, 20.0, 30.0))
    report.append("[after translate((10,20,30))]")
    report.append(_probe_call(b, "extents")); report.append(_probe_call(b, "transformationMatrix"))
    b.rotateZ(90)
    report.append("[after rotateZ(90)]")
    report.append(_probe_call(b, "extents")); report.append(_probe_call(b, "transformationMatrix"))
    b.rotateX(90)
    report.append("[after rotateX(90)]")
    report.append(_probe_call(b, "extents")); report.append(_probe_call(b, "transformationMatrix"))
    try:
      b.erase()
    except Exception:
      pass
  except Exception as e:
    report.append("TRANSFORM ERR: " + str(e))

  where = _probe_write("\n".join(report))
  try:
    print("PRIMITIV_PROBE written to: " + str(where))
  except Exception:
    pass
# ===========================================================================


@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member profile")
@param(A=LENGTH, TooltipLong="A length")
@param(A1=LENGTH, TooltipLong="A1 length")
@param(Ha=LENGTH, TooltipLong="Ha length")
@param(Hb=LENGTH, TooltipLong="Hb length")
@param(B1=LENGTH, TooltipLong="B1 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|TYPE D = -4|\
  TYPE E = -5")

def RS6(s, Dn = 100, BI = 210, A = 400.0, A1 = 400.0,
  Ha = 500.0, Hb = 500.0, B1 = 0.0, P1 = 0.0,
  TY = -2, ID = "RS6", **kwargs):
  """
  """
  # ===== PRIMITIV_PROBE (임시 관측; 끝나면 이 블록과 위 헬퍼 제거) =====
  _probe_run(s)
  BOX(s, L = 300.0, W = 300.0, H = 300.0)   # 가시 마커: 이게 보이면 실행 성공
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  return
  # ===== 관측 끝 (아래는 원본 RS6 로직, 위 return 으로 비활성) =====

  valid_member = [16, 17, 210, 212, 215, 312, 315]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    return
