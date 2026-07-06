## RS15
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
@param(A1=LENGTH, TooltipLong="A1 length")
@param(A2=LENGTH, TooltipLong="A2 length")
@param(L1=LENGTH, TooltipLong="L1 length")
@param(F2=LENGTH, TooltipLong="F1 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def RS15(s, Dn = 100, BI = 315, A1 = 300.0, A2 = 300.0, P1 = 0.0,
  L1 = 50.0, F2 = 600.0, TY = -2, ID = "RS15", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [110, 215, 220, 315]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 110, 215, 220, 315")
    return

  if (TY == -1):
    if (BI != 110):
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Member 'M' size should be 'A10' for TYPE A!")
      return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  Dz = pipe_size[Dn]
  F1 = A1 + A2

  if (TY == -1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2 - 10.0)
    m1.rotateY(90).translate((-A1, 0.0, 0.0))
    m2.rotateZ(180).translate((A2 - 10.0, 0.0, -F2))

    m1.uniteWith(m2)
    m2.erase()
    sPL1 = [(0.0, Fa1 + Dz/2 + P1, 0.0), (-A1, Fa1 + Dz/2 + P1, 0.0)]
    sPL2 = [(0.0, Fa1 + Dz/2 + P1, 0.0), (A2, Fa1 + Dz/2 + P1, 0.0)]
    sPL3 = [(A2, Dz/2 + P1, 0.0), (A2, Dz/2 + P1, -F2)]

    sP1 = (A2 - 10.0, Dz/2 + P1, -F2)
    sP2 = (-A1, Dz/2 + P1, 0.0)

  elif (TY == -2):
    if (BPn == 1):
      dF3 = F1 - L1 - 10.0
      F3 = dF3 / (sqrt(2) / 2)
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2 - 10)
      m3 = L_SHAPE(s, BF = 7, LE = F3)
      m1.rotateY(90).translate((-A1, 0.0, -Dz/2 - P1))
      m2.rotateZ(180).translate((A2 - 10.0, 0.0, -F2 - Dz/2 - P1))
      m3.rotateZ(180).rotateY(-45)
      m3.translate((A2 - 10.0, 0.0, -dF3 - Dz/2 - P1))

      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m2.erase()
      m3.erase()

      sPL1 = [(0.0, Fa1, -Dz/2 - P1), (-A1, Fa1, -Dz/2 - P1)]
      sPL2 = [(0.0, Fa1, -Dz/2 - P1), (A2, Fa1, -Dz/2 - P1)]
      sPL3 = [(A2, 0.0, -Dz/2 - P1), (A2, 0.0, -F2 - Dz/2 - P1)]

      sP1 = (A2 - 10.0, 0.0, -Dz/2 - P1 - F2)
      sP2 = (-A1, 0.0, -Dz/2 - P1)
      sP3 = (A2 - 10.0 - dF3/2, 0.0, -Dz/2 - P1 - dF3/2)

    elif (BPn == 2):
      Fa1 = profile[BI]["Fa1"]
      Fa2 = profile[BI]["Fa2"]
      dF3 = F1 - L1 - Fa1 - 10.0
      F3 = dF3 / (sqrt(2) / 2)
      m1 = C_SHAPE(s, BF = BF, LE = F1)
      m2 = C_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = 7, LE = F3)
      m1.rotateZ(-90).rotateY(90)
      m1.translate((-A1, 0.0, -Dz/2 - Fa1/2 - P1))
      m2.rotateZ(-90)
      m2.translate((A2 - 10.0 - Fa1/2, 0.0, -F2 - Dz/2 - Fa1 - P1))

      mb31 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "TOP_04")
      mb32 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_03")
      mb31.rotateY(45).translate((75, 0.0, 0.0))
      mb32.rotateY(-45).translate((75, 0.0, F3))
      m3.subtractFrom(mb31)
      m3.subtractFrom(mb32)
      mb31.erase()
      mb32.erase()
      m3.rotateZ(180).translate((75, 0.0, 0.0)).rotateY(-45)
      dFa2 = (Fa2 - 75.0) / 2
      m3.translate((A2 - 10.0 - Fa1, -dFa2, -dF3 - Dz/2 - P1 - Fa1))

      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m2.erase()
      m3.erase()

      sPL1 = [(0.0, -Fa2, -Dz/2 - P1), (-A1, -Fa2, -Dz/2 - P1)]
      sPL2 = [(0.0, -Fa2, -Dz/2 - P1), (A2, -Fa2, -Dz/2 - P1)]
      sPL3 = [(A2, 0.0, -Dz/2 - P1), (A2, 0.0, -F2 - Fa1 - Dz/2 - P1)]

      sP1 = (A2 - 10.0 - Fa1/2, 0.0, -Dz/2 - P1 - Fa1 - F2)
      sP2 = (-A1, 0.0, -Dz/2 - P1)
      sP3 = (A2 - 10.0 - Fa1 - dF3/2, 0.0, -Dz/2 - P1 - Fa1 - dF3/2)

    elif (BPn == 3):
      Fa1 = profile[BI]["F"]
      T = profile[BI]["T"]
      dF3 = F1 - L1 - Fa1 - 10.0
      F3 = dF3 / (sqrt(2) / 2)
      m1 = H_SHAPE(s, BF = BF, LE = F1)
      m2 = H_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = 7, LE = F3)
      m1.rotateY(90).translate((-A1, 0.0, -Dz/2 - Fa1/2 - P1))
      m2.translate((A2 - 10.0 - Fa1/2, 0.0, -F2 - Dz/2 - Fa1 - P1))

      mb31 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "TOP_04")
      mb32 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_03")
      mb31.rotateY(45).translate((75.0, 0.0, 0.0))
      mb32.rotateY(-45).translate((75.0, 0.0, F3))
      m3.subtractFrom(mb31)
      m3.subtractFrom(mb32)
      mb31.erase()
      mb32.erase()
      m3.rotateZ(180).translate((75.0, 0.0, 0.0)).rotateY(-45)
      m3.translate((A2 - 10.0 - Fa1, 75/2, -dF3 - Dz/2 - P1 - Fa1))

      m4pl1 = TRANS_BOX(s, L = Fa1-20.0, W = Fa1, H = 10.0, BASE = "BOTTOM_05")
      m4pl2 = TRANS_BOX(s, L = Fa1-20.0, W = Fa1, H = 10.0, BASE = "BOTTOM_05")
      m4pl1.translate((A2 - 10.0 - T/2, 0.0, -Fa1 - Dz/2 - P1))
      m4pl2.translate((A2 - 10.0 - Fa1 + T/2, 0.0, -Fa1 - Dz/2 - P1))

      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m1.uniteWith(m4pl1)
      m1.uniteWith(m4pl2)
      m2.erase()
      m3.erase()
      m4pl1.erase()
      m4pl2.erase()

      sPL1 = [(0.0, Fa1/2, -Dz/2 - P1), (-A1, Fa1/2, -Dz/2 - P1)]
      sPL2 = [(0.0, Fa1/2, -Dz/2 - P1), (A2, Fa1/2, -Dz/2 - P1)]
      sPL3 = [(A2, 0.0, -Dz/2 - P1), (A2, 0.0, -F2 - Fa1 - Dz/2 - P1)]

      sP1 = (A2 - 10.0 - Fa1/2, 0.0, -Dz/2 - P1 - Fa1 - F2)
      sP2 = (-A1, 0.0, -Dz/2 - P1)
      sP3 = (A2 - 10.0 - Fa1 - dF3/2, 0.0, -Dz/2 - P1 - Fa1 - dF3/2)
      sP4 = (A2 - 10.0 - T/2, -Fa1/2, -Dz/2 - P1 - Fa1/2)

  # Set point
  if (TY == -1):
    m1.translate((0.0, Dz/2 + P1, 0.0))
    s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

    # For F2
    s.setPoint(sP1, (0.0, 0.0, 1.0))
    # For F1
    s.setPoint(sP2, (0.0, 0.0, 1.0))

    s.setLinearDimension('A1', sPL1[0], sPL1[1])
    s.setLinearDimension('A2', sPL2[0], sPL2[1])
    s.setLinearDimension('F2', sPL3[0], sPL3[1])

  elif (TY == -2):
    s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

    # For F2
    s.setPoint(sP1, (0.0, 0.0, 1.0))
    # For F1
    s.setPoint(sP2, (0.0, 0.0, 1.0))
    # For F3
    s.setPoint(sP3, (0.0, 0.0, 1.0))
    # For P1 stiffener
    if (BPn == 3):
      s.setPoint(sP4, (0.0, 0.0, 1.0))

    s.setLinearDimension('A1', sPL1[0], sPL1[1])
    s.setLinearDimension('A2', sPL2[0], sPL2[1])
    s.setLinearDimension('F2', sPL3[0], sPL3[1])




