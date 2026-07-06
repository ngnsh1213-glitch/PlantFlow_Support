## PRIMITIV_PROBE
## 목적: PFS 프리뷰 B(Phase 0) — varmain.primitiv 프리미티브 좌표/회전/구조 시맨틱 런타임 관측 덤프.
## 등록: RS6.py 헤더/데코레이터/파라미터/시그니처를 그대로 복사(등록·배치 동작 동일 보장).
##       본문만 프리미티브 관측 + 가시 마커 박스로 교체.
## 사용: 테스트 .acat/.pspc 변형(19404,15A) ContentGeometryTemplate='PRIMITIV_PROBE' → PLANTREGISTERCUSTOMSCRIPTS → 배치.
## 결과: 큰 박스(실행 확인) + primitiv_probe.txt(바탕화면/TEMP/C:\Temp).
##--------------------------------
from aqa.math import *
from varmain.primitiv import *
from varmain.var_basic import *
from varmain.custom import *

import os
import sys
directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)


# ---- 출력 채널 (모두 가드) ---------------------------------------------------
def _candidate_paths():
  paths = []
  try:
    paths.append(os.path.join(os.path.expanduser("~"), "Desktop", "primitiv_probe.txt"))
  except Exception:
    pass
  try:
    t = os.environ.get("TEMP") or os.environ.get("TMP")
    if t:
      paths.append(os.path.join(t, "primitiv_probe.txt"))
  except Exception:
    pass
  paths.append(r"C:\Temp\primitiv_probe.txt")
  paths.append("primitiv_probe.txt")
  return paths


def _write_report(text):
  for p in _candidate_paths():
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
    for p in _candidate_paths():
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


def _safe_repr(v):
  try:
    r = repr(v)
  except Exception as e:
    return "REPR_ERR:" + str(e)
  if len(r) > 300:
    r = r[:300] + "...(trunc)"
  return r


def _dump_obj(obj):
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
    if callable(val):
      lines.append("  ." + a + "()  [callable]")
    else:
      lines.append("  ." + a + " = " + _safe_repr(val))
  for cand in ("extents", "Extents", "bbox", "BoundingBox", "GeometricExtents",
               "min", "max", "Min", "Max", "center", "Center", "origin",
               "Origin", "solid", "Solid", "id", "Id", "ObjectId", "handle", "Handle"):
    try:
      if hasattr(obj, cand):
        lines.append("  >> " + cand + " = " + _safe_repr(getattr(obj, cand)))
    except Exception:
      pass
  return "\n".join(lines)


def _try_primitive(report, name, fn):
  report.append("=" * 60)
  report.append("PRIMITIVE: " + name)
  try:
    obj = fn()
    report.append(_dump_obj(obj))
    try:
      obj.erase()
    except Exception as e:
      report.append("  (erase failed: " + str(e) + ")")
  except Exception as e:
    report.append("  CONSTRUCT_ERR: " + str(e))


def _dump_module(report):
  report.append("=" * 60)
  report.append("MODULE varmain.primitiv 심볼:")
  try:
    import varmain.primitiv as P
    for a in dir(P):
      if a.startswith("__"):
        continue
      try:
        v = getattr(P, a)
        report.append("  " + a + " : " + ("callable" if callable(v) else _safe_repr(v)))
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


# ---- RS6.py 와 동일한 등록 데코레이터/파라미터/시그니처 ---------------------
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

def PRIMITIV_PROBE(s, Dn = 100, BI = 210, A = 400.0, A1 = 400.0,
  Ha = 500.0, Hb = 500.0, B1 = 0.0, P1 = 0.0,
  TY = -2, ID = "PRIMITIV_PROBE", **kwargs):
  """varmain.primitiv 런타임 관측 + 가시 마커 박스."""
  report = []
  report.append("PRIMITIV_PROBE report")
  try:
    report.append("python.version = " + str(getattr(sys, "version", "?")))
  except Exception as e:
    report.append("sys ERR: " + str(e))

  _dump_module(report)

  _try_primitive(report, "BOX(L=100,W=200,H=50)",
                 lambda: BOX(s, L=100.0, W=200.0, H=50.0))
  _try_primitive(report, "CYLINDER(R=50,H=300,O=0)",
                 lambda: CYLINDER(s, R=50.0, H=300.0, O=0.0))
  _try_primitive(report, "CYLINDER(R1=60,R2=30,H=300,O=0)",
                 lambda: CYLINDER(s, R1=60.0, R2=30.0, H=300.0, O=0.0))
  _try_primitive(report, "TORUS(R1=100,R2=20)",
                 lambda: TORUS(s, R1=100.0, R2=20.0))
  _try_primitive(report, "SPHERE(R=80)",
                 lambda: SPHERE(s, R=80.0))

  report.append("=" * 60)
  report.append("TRANSFORM: BOX(100,100,100) translate((10,20,30)) -> rotateZ(90)")
  try:
    b = BOX(s, L=100.0, W=100.0, H=100.0)
    report.append("[before]")
    report.append(_dump_obj(b))
    try:
      b.translate((10.0, 20.0, 30.0))
      report.append("[after translate]")
      report.append(_dump_obj(b))
    except Exception as e:
      report.append("translate ERR: " + str(e))
    try:
      b.rotateZ(90)
      report.append("[after rotateZ(90)]")
      report.append(_dump_obj(b))
    except Exception as e:
      report.append("rotateZ ERR: " + str(e))
    try:
      b.erase()
    except Exception:
      pass
  except Exception as e:
    report.append("TRANSFORM CONSTRUCT_ERR: " + str(e))

  where = _write_report("\n".join(report))
  try:
    print("PRIMITIV_PROBE written to: " + str(where))
  except Exception:
    pass

  # 가시 마커 박스(erase 안 함) — 배치 시 보이면 probe 실행 성공.
  try:
    BOX(s, L=300.0, W=300.0, H=300.0)
  except Exception as e:
    try:
      print("marker box ERR: " + str(e))
    except Exception:
      pass

  # RS6 패턴의 삽입점(파트 유효성).
  try:
    s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  except Exception:
    pass
