## PRIMITIV_PROBE_V3
## 목적: PFS 서버사이드 메시 export 스파이크 1-A.
## 결과 엔티티/런타임 객체의 handle, ObjectId 후보, 실타입 후보를 최대한 덤프한다.
from aqa.math import *
from varmain.primitiv import *
from varmain.var_basic import *
from varmain.custom import *

import os
import sys

directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)

_probe_notes = []


def _note(message):
  try:
    _probe_notes.append(str(message))
  except Exception as exc:
    print("PRIMITIV_PROBE_V3 note failure: " + str(exc))


def _probe_paths():
  paths = []
  try:
    paths.append(os.path.join(os.path.expanduser("~"), "Desktop", "primitiv_probe_v3.txt"))
  except Exception as exc:
    _note("Desktop path ERR: " + str(exc))
  try:
    t = os.environ.get("TEMP") or os.environ.get("TMP")
    if t:
      paths.append(os.path.join(t, "primitiv_probe_v3.txt"))
  except Exception as exc:
    _note("TEMP path ERR: " + str(exc))
  paths.append(r"C:\Temp\primitiv_probe_v3.txt")
  paths.append("primitiv_probe_v3.txt")
  return paths


def _write_report(text):
  for path in _probe_paths():
    try:
      f = open(path, "w")
      try:
        f.write(text)
      finally:
        f.close()
      return "PY:" + str(path)
    except Exception as exc:
      _note("write path failed " + str(path) + ": " + str(exc))
      continue
  try:
    from System.IO import File
    for path in _probe_paths():
      try:
        File.WriteAllText(path, text)
        return "DOTNET:" + str(path)
      except Exception as exc:
        _note("dotnet write path failed " + str(path) + ": " + str(exc))
        continue
  except Exception as exc:
    _note("System.IO.File import/write ERR: " + str(exc))
  try:
    print(text)
  except Exception as exc:
    _note("print fallback ERR: " + str(exc))
  return "PRINT_ONLY"


def _safe_repr(value):
  try:
    text = repr(value)
  except Exception as exc:
    return "REPR_ERR:" + str(exc)
  if len(text) > 500:
    text = text[:500] + "...(trunc)"
  return text


def _get_attr(obj, name):
  try:
    if hasattr(obj, name):
      return getattr(obj, name)
  except Exception as exc:
    return "GET_ERR:" + str(exc)
  return None


def _call_zero(obj, name):
  try:
    member = getattr(obj, name, None)
    if callable(member):
      return member()
  except Exception as exc:
    return "CALL_ERR:" + str(exc)
  return None


def _dump_obj(label, obj):
  lines = []
  lines.append("=" * 60)
  lines.append(label)
  try:
    lines.append("type = " + str(type(obj)))
  except Exception as exc:
    lines.append("type = ERR:" + str(exc))

  candidates = (
    "Handle", "handle", "ObjectId", "objectId", "Id", "id",
    "ClassName", "className", "Name", "name", "EntityName",
    "solid", "Solid", "Body", "body", "Geometry", "geometry",
    "extents", "Extents", "GeometricExtents", "BoundingBox", "bbox",
    "CurrentSpace", "currentSpace", "Database", "database"
  )
  for cand in candidates:
    value = _get_attr(obj, cand)
    if value is not None:
      lines.append(cand + " = " + _safe_repr(value))

  calls = (
    "handle", "objectId", "type", "className", "extents",
    "transformationMatrix", "parameters", "numberOfPoints"
  )
  for name in calls:
    value = _call_zero(obj, name)
    if value is not None:
      lines.append(name + "() = " + _safe_repr(value))

  try:
    names = dir(obj)
    interesting = []
    for name in names:
      low = name.lower()
      if "handle" in low or "object" in low or "solid" in low or "body" in low or "geom" in low or "space" in low:
        interesting.append(name)
    lines.append("interesting_dir = " + _safe_repr(interesting[:100]))
  except Exception as exc:
    lines.append("dir ERR: " + str(exc))

  return "\n".join(lines)


def _dump_current_space(report, s):
  report.append(_dump_obj("CURRENT SPACE / SCRIPT CONTEXT", s))
  for name in ("CurrentSpace", "currentSpace", "space", "Space", "Database", "database"):
    value = _get_attr(s, name)
    if value is not None:
      report.append(_dump_obj("s." + name, value))


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
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|TYPE D = -4|TYPE E = -5")
def PRIMITIV_PROBE_V3(s, Dn=100, BI=210, A=400.0, A1=400.0,
  Ha=500.0, Hb=500.0, B1=0.0, P1=0.0, TY=-2, ID="PRIMITIV_PROBE_V3", **kwargs):
  report = []
  report.append("PRIMITIV_PROBE report v3")
  report.append("goal = capture real entity type / handle candidates before C# tessellation spike")
  try:
    report.append("python.version = " + str(getattr(sys, "version", "?")))
  except Exception as exc:
    report.append("python.version ERR: " + str(exc))

  created = []
  try:
    box = BOX(s, L=300.0, W=300.0, H=300.0)
    created.append(("marker_BOX_300", box))
  except Exception as exc:
    report.append("marker BOX create ERR: " + str(exc))

  try:
    cyl = CYLINDER(s, R=50.0, H=200.0, O=0.0)
    created.append(("marker_CYLINDER", cyl))
  except Exception as exc:
    report.append("marker CYLINDER create ERR: " + str(exc))

  for label, obj in created:
    report.append(_dump_obj(label, obj))

  _dump_current_space(report, s)

  try:
    s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  except Exception as exc:
    report.append("setPoint ERR: " + str(exc))

  if _probe_notes:
    report.append("=" * 60)
    report.append("probe_notes")
    for note in _probe_notes:
      report.append(str(note))

  where = _write_report("\n".join(report))
  try:
    print("PRIMITIV_PROBE_V3 written to: " + str(where))
  except Exception as exc:
    _note("final print ERR: " + str(exc))
