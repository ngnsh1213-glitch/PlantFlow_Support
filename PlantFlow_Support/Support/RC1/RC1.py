## RC1
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

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_RC1 as baseplate

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
@param(B1=LENGTH, TooltipLong="Plate location")
@param(B2=LENGTH, TooltipLong="B param")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def RC1(s, Dn = 100, BI = 15, A = 350.0, A1 = 100.0,
  F2 = 500.0, P1 = 0.0, B1 = 0, B2 = 0.0, TY = -1, ID = "RC1", **kwargs):
  """
  Resting Component Support RC1
  """
  # Check member profile as standard
  valid_member = [15, 16, 17, 210]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 15, 16, 17, 210")
    return

  if (TY not in [-1, -2]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: -1, -2")
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
  positions = [
    (plB/2,  plB/2,  -plt/2), (-plB/2, plB/2,  -plt/2),
    (-plB/2, -plB/2, -plt/2), (plB/2,  -plB/2, -plt/2),
  ]
  for pos in positions:
    cy1 = CYLINDER(s, R = plh/2, H = plt, O = 0.0)
    cy1.translate(pos)
    pl1.subtractFrom(cy1)
    cy1.erase()

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2 + Fa1)

    m1.rotateZ(-90).rotateY(90).translate((-F1 + 10.0, 0.0, 0.0))
    m2.rotateZ(90).translate((0.0, 0.0, -Fa1))

    if (TY == -1):
      if (B1 not in [0, 90]):
        m1.erase()
        m2.erase()
        pl1.erase()
        e1 = ERROR_SHAPE(s, R = 100.0)
        print("B1 should be 0 or 90 only!")
        return
      
      if (B1 == 0):
        pl1.rotateY(90).translate((plt/2, Fa1/2, F2 - plA/2 + Fa1/2))
      elif (B1 == 90):
        pl1.rotateX(90).translate((-Fa1/2, -plt/2, F2 - plA/2 + Fa1/2))

      sP4 = (A - 10.0 + plt, 0.0, F2 - Dz/2 - P1 - plA/2)

    elif (TY == -2):
      pl1.translate((-Fa1/2, Fa1/2, F2 - plt/2))
      sP4 = (A - 10.0 - Fa1/2 + plA/2, 0.0, F2 - Dz/2 - P1)

    m1.uniteWith(m2)
    m1.uniteWith(pl1)
    m2.erase()
    pl1.erase()

    m1.translate((A - 10.0, 0.0, -Dz/2 - P1))

    sP2 = (A - 10.0, 0.0, F2 - Dz/2 - P1)
    sP3 = (-A1 , 0.0, -Dz/2 - P1)
    sPA1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
    sPA = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]
    sPF2 = [(A, 0.0, -Dz/2 - P1), (A, 0.0, F2 - Dz/2 - P1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    T1 = profile[BI]["T1"]
    T2 = profile[BI]["T2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = F2)

    if (TY == -1):
      m1.erase()
      m2.erase()
      pl1.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Profile C cannot use side base plate!")
      return

    m1.rotateZ(-90).rotateY(90).translate((-F1 + 10.0 + Fa1/2, 0.0, -Fa1/2))
    m2.rotateZ(-90)

    pl1.translate((0.0, -Fa2/2, F2 - plt/2))
    m1.uniteWith(m2)
    m1.uniteWith(pl1)
    m2.erase()
    pl1.erase()

    m1.translate((A - 10.0 - Fa1/2, 0.0, -Dz/2 - P1))

    sP2 = (A - 10.0 - Fa1/2, 0.0, F2 - Dz/2 - P1)
    sP3 = (-A1, 0.0, -Dz/2 - P1)
    sP4 = (A + 10.0, 0.0, F2 - Dz/2 - P1)
    sPA1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
    sPA = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]
    sPF2 = [(A, 0.0, -Dz/2 - P1), (A, 0.0, F2 - Dz/2 - P1)]

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

  # For F2 and datum
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  # For F1
  s.setPoint(sP3, (0.0, 0.0, 1.0))
  # For P1
  s.setPoint(sP4, (0.0, 0.0, 1.0))
  
  s.setLinearDimension('A', sPA[0], sPA[1])
  s.setLinearDimension('A1', sPA1[0], sPA1[1])
  s.setLinearDimension('F2', sPF2[0], sPF2[1])
