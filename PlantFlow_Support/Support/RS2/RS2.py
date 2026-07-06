## RS2
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

from support_data import pipe_size_JIS as pipe_size, profile

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member profile")
@param(A=LENGTH, TooltipLong="A1 length")
@param(F2=LENGTH, TooltipLong="F2 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|TYPE D = -4|")

def RS2(s, Dn = 25, BI = 210, A = 800.0, F2 = 900.0, P1 = 0.0,
  TY = -2, ID = "RS2", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [16, 17, 210, 212, 215, 312, 315]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 16, 17, 210, 212, 215, 312, 315")
    return

  if (TY not in [-1, -2, -3, -4]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: -1, -2, -3, -4")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  Dz = pipe_size[Dn]
  F1 = A + Dz/2 + 100.0
  A1 = 150.0

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2)
    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(180)
    if (TY in [-2, -3, -4]):
      Fa1 = profile[BI]["F"]
      sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
      sb2.rotateY(-45).translate((0.0, 0.0, F2 - Fa1))
      m2.subtractFrom(sb2)
      sb2.erase()

    m2.rotateY(-135).translate((F1 - A1, 0.0, 0.0))
    m1.uniteWith(m2)
    m2.erase()

    sP2 = (-A, 0.0, -Dz/2 - P1)
    sP3 = (-(F2*(sqrt(2) / 2) + A1 - Dz/2 - 100.0), 
           0.0, -Dz/2 - P1 - F2*(sqrt(2) / 2))
    sPL = [(0.0, -Fa1, -Dz/2 - P1), (-A, -Fa1, -Dz/2 - P1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = F2)
    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(-90).translate((Fa1/2, 0.0, 0.0))
    sb1 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb1.rotateY(45)
    m2.subtractFrom(sb1)
    sb1.erase()
    if (TY in [-2, -3, -4]):
      sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
      sb2.rotateY(45).translate((0.0, 0.0, F2))
      m2.subtractFrom(sb2)
      sb2.erase()

    m2.rotateY(-135).translate((F1-A1, 0.0, -Fa1/2))
    m1.uniteWith(m2)
    m2.erase()
    m1.translate((0.0, 0.0, -Fa1/2))

    sP2 = (-A, 0.0, -Dz/2 - P1 - Fa1/2)
    sP3 = (-(F2*(sqrt(2) / 2) + A1 - Dz/2 - 100.0), 
           0.0, -Dz/2 - P1 - F2*(sqrt(2) / 2) - Fa1)
    sPL = [(0.0, 0.0, -Dz/2 - P1), (-A, 0.0, -Dz/2 - P1)]

  elif  (BPn == 3):
    Fa1 = profile[BI]["F"]
    m1 = H_SHAPE(s, BF = BF, LE = F1)
    m2 = H_SHAPE(s, BF = BF, LE = F2)
    m1.rotateY(90)
    m2.translate((Fa1/2, 0.0, 0.0))
    sb1 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb1.rotateY(45)
    m2.subtractFrom(sb1)
    sb1.erase()
    if (TY in [-2, -3, -4]):
      sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
      sb2.rotateY(45).translate((0.0, 0.0, F2))
      m2.subtractFrom(sb2)
      sb2.erase()

    m2.rotateY(-135).translate((F1-A1, 0.0, -Fa1/2))
    m1.uniteWith(m2)
    m2.erase()
    m1.translate((0.0, 0.0, -Fa1/2))

    sP2 = (-A, 0.0, -Dz/2 - P1 - Fa1/2)
    sP3 = (-(F2*(sqrt(2) / 2) + A1 - Dz/2 - 100.0), 
           0.0, -Dz/2 - P1 - F2*(sqrt(2) / 2) - Fa1)
    sPL = [(0.0, 0.0, -Dz/2 - P1), (-A, 0.0, -Dz/2 - P1)]

  m1.translate((-A, 0.0, -Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

  # For F1 and datum
  s.setPoint(sP2, (0.0, 0.0, 1.0))

  # For F2
  s.setPoint(sP3, (0.0, 0.0, 1.0))

  # For dim
  s.setPoint((Dz/2 + 100.0, 0.0, -Dz/2-P1), (0.0, 0.0, 1.0))

  s.setLinearDimension('A', sPL[0], sPL[1])



