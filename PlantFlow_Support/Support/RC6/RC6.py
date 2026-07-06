## RC6
## Developed by Noh Seunghwan
## Last Modified: Apr 29, 2026
## V:\4_부서관리\9_배관팀\03 설계자료\03_02 Support\PIPE SUPPORT STANDARD_R0_260311
## Linked Backend: [[scan_HANTEC_cs_20260426_144159|HANTEC]], [[scan_BOMs_cs_20260426_144100|BOMs]], [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
##--------------------------------
from aqa.math import *
from varmain.primitiv import * 
from varmain.var_basic import * 
from varmain.custom import *

import os
import sys
directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_RC5_6 as baseplate

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *


@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member profile")
@param(A=LENGTH, TooltipLong="A length")
@param(A1=LENGTH, TooltipLong="A1 length")
@param(F2=LENGTH, TooltipLong="F2 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|")

def RC6(s, Dn = 100, BI = 15, A = 400.0, A1 = 250.0, 
  F2 = 600.0, P1 = 0.0, TY = -1, ID = "RC6", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [15, 16, 17, 210, 212, 215]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 15, 16, 17, 210, 212, 215")
    return

  if (TY not in [-1, -2, -3]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: -1, -2, -3")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  Dz = pipe_size[Dn]
  F1 = A + A1

  plA = baseplate[BI]["A"]
  plB = baseplate[BI]["B"]
  plt = baseplate[BI]["t"]
  plh = baseplate[BI]["h"]
  pl1 = BOX(s, L = plA, W = plt, H = plA)
  pl2 = BOX(s, L = plA, W = plt, H = plA)
  positions = [
    (plB/2,  plB/2,  -plt/2),
    (-plB/2, plB/2,  -plt/2),
    (-plB/2, -plB/2, -plt/2),
    (plB/2,  -plB/2, -plt/2),
  ]
  for pos in positions:
    cy1 = CYLINDER(s, R = plh/2, H = plt, O = 0.0)
    cy1.translate(pos)
    pl1.subtractFrom(cy1)
    cy1.erase()

    cy2 = CYLINDER(s, R = plh/2, H = plt, O = 0.0)
    cy2.translate(pos)
    pl2.subtractFrom(cy2)
    cy2.erase()

  if (TY == -1):
    if (BI not in [15, 16, 17]):
      pl1.erase()
      pl2.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Only A5, A6, A7 for TYPE-A!")
      return

    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2 - T1)

    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(-90).translate((0.0, -T1, -F2))

    pl1.rotateY(90).translate((F1 - plt/2, -Fa1/2, -Fa1/2))
    pl2.translate((Fa1/2, -T1 - Fa1/2, plt/2 - F2))
    m1.uniteWith(m2)
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    m2.erase()
    pl1.erase()
    pl2.erase()

    sP2 = (-A1, -T1, -Dz/2 - P1 - F2)
    sP3 = (-A1, 0.0, - Dz/2 - P1)
    sP4 = (-A1 - plA/2 + Fa1/2, 0.0, -Dz/2 - P1 - F2)
    sP5 = (A, 0.0, - Dz/2 - P1 - Fa1/2 + plA/2)
    sLD = [(-A1, -Fa1, -Dz/2 - P1), (-A1, -Fa1, -F2 - Dz/2 - P1)]

  elif (TY == -2 or TY == -3):
    if (BPn == 1):
      Fa1 = profile[BI]["F"]
      T1 = profile[BI]["T"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2)

      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(180).translate((0.0, 0.0, -F2))

      pl1.rotateY(90).translate((F1 - plt/2, -Fa1/2, -Fa1/2))
      pl2.translate((-Fa1/2, -Fa1/2, plt/2 - F2))
      m1.uniteWith(m2)
      m1.uniteWith(pl1)
      m1.uniteWith(pl2)
      m2.erase()
      pl1.erase()
      pl2.erase()

      sP2 = (-A1, 0.0, -Dz/2 - P1 - F2)
      sP3 = (A, 0.0, -Dz/2 - P1 - Fa1/2)
      sP4 = (-A1 - plA/2 - Fa1/2, 0.0, -Dz/2 - P1 - F2)
      sP5 = (A, 0.0, - Dz/2 - P1 - Fa1/2 + plA/2)
      sLD = [(-A1 - Fa1/2, 0.0, -Dz/2 - P1), 
             (-A1 - Fa1/2, 0.0, -F2 - Dz/2 - P1)]

    elif (BPn == 2):
      Fa1 = profile[BI]["Fa1"]
      Fa2 = profile[BI]["Fa2"]
      T1 = profile[BI]["T1"]
      T2 = profile[BI]["T2"]
      m1 = C_SHAPE(s, BF = BF, LE = F1)
      m2 = C_SHAPE(s, BF = BF, LE = F2)

      m1.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -Fa1/2))
      m2.rotateZ(-90).translate((-Fa1/2, 0.0, -F2))
      pl1.rotateY(90).translate((F1 - plt/2, -Fa2/2, -Fa1/2))
      pl2.translate((-Fa1/2, -Fa2/2, plt/2 - F2))

      m1.uniteWith(m2)
      m1.uniteWith(pl1)
      m1.uniteWith(pl2)
      m2.erase()
      pl1.erase()
      pl2.erase()

      sP2 = (-A1 - Fa1/2, 0.0, -Dz/2 - P1 - F2)
      sP3 = (A, 0.0, -Dz/2 - P1 - Fa1/2)
      sP4 = (-A1 - Fa1/2 - plA/2, 0.0, -Dz/2 - P1 - F2)
      sP5 = (A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2)
      sLD = [(-A1, 0.0, -Dz/2 - P1), (-A1, 0.0, -F2 - Dz/2 - P1)]

  m1.translate((-A1, 0.0, -Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  # For F2 and datumn
  s.setPoint(sP2, (0.0, 1.0, 0.0))
  # For F1
  s.setPoint(sP3, (0.0, 1.0, 0.0))
  # For P1_1 and P1_2
  s.setPoint(sP4, (0.0, 1.0, 0.0))
  s.setPoint(sP5, (0.0, 1.0, 0.0))
  s.setPoint((0.0, 0.0, -Dz/2-P1), (0.0, 1.0, 0.0))
  s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1))
  s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1))
  s.setLinearDimension('F2', sLD[0], sLD[1])

