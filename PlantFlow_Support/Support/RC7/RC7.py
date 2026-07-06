## RC7
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

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_RC7 as baseplate

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *


@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member profile")
@param(A=LENGTH, TooltipLong="A1 length")
@param(A1=LENGTH, TooltipLong="A1 length")
@param(P1=LENGTH, TooltipLong="P1 length")

def RC7(s, Dn = 25, BI = 210, A = 800.0, A1 = 150.0, 
  P1 = 0.0, ID = "RC7", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [16, 17, 210, 212, 215, 312, 315]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 16, 17, 210, 212, 215, 312, 315")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  A2 = 150.0

  Dz = pipe_size[Dn]
  F1 = A + A1
  F2 = (F1 - A2) / (sqrt(2) / 2)

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
    
  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2 + Fa1)
    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(180)

    sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb2.rotateY(-45).translate((0.0, 0.0, F2))
    m2.subtractFrom(sb2)
    sb2.erase()

    m2.rotateY(-135).translate((F1 - A2, 0.0, 0.0))
    pl1.rotateY(90).translate((plt/2, -Fa1/2, -Fa1/2))
    pl2.rotateY(90).translate((plt/2, -Fa1/2, A2 - Fa1/2 - F1))
    m1.uniteWith(m2)
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    m2.erase()
    pl1.erase()
    pl2.erase()

    sP2 = (-A, 0.0, -Dz/2 - P1)
    sP3 = (A1 - A2, 0.0, -Dz/2 - P1 - Fa1 - 10.0)
    sP4 = (-A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2)
    sP5 = (-A, 0.0, -Dz/2 - P1 - Fa1/2 - F1 + A2 - plA/2)
    sPLA = [(0.0, -Fa1, -Dz/2 - P1), (-A, -Fa1, -Dz/2 - P1)]
    sPLA1 = [(0.0, -Fa1, -Dz/2 - P1), (A1, -Fa1, -Dz/2 - P1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = F2)
    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(-90).translate((Fa1/2, 0.0, 0.0))
    sb1 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb1.rotateY(45)
    m2.subtractFrom(sb1)
    sb1.erase()

    sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb2.rotateY(45).translate((0.0, 0.0, F2))
    m2.subtractFrom(sb2)
    sb2.erase()

    m2.rotateY(-135).translate((F1-A2, 0.0, -Fa1/2))
    m1.uniteWith(m2)
    m2.erase()
    m1.translate((0.0, 0.0, -Fa1/2))

    di1 = (Fa1 / (sqrt(2)/2)) / 2
    pl1.rotateY(90).translate((plt/2, -Fa2/2, -Fa1/2))
    pl2.rotateY(90).translate((plt/2, -Fa2/2, -Fa1 - F1 + A2 + di1))
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    pl1.erase()
    pl2.erase()

    sP2 = (-A, 0.0, -Dz/2 - P1 - Fa1/2)
    sP3 = (A1 - A2, 0.0, -Dz/2 - P1 - Fa1)
    sP4 = (-A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2)
    sP5 = (-A, 0.0, -Dz/2 - P1 - Fa1 - (F1 - A2) + di1 - plA/2)
    sPLA = [(0.0, -Fa2, -Dz/2 - P1), (-A, -Fa2, -Dz/2 - P1)]
    sPLA1 = [(0.0, -Fa2, -Dz/2 - P1), (A1, -Fa2, -Dz/2 - P1)]

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

    sb2 = TRANS_BOX(s, L = Fa1*Fa1, W = Fa1*Fa1, H = Fa1*Fa1, BASE = "BOTTOM_06")
    sb2.rotateY(45).translate((0.0, 0.0, F2))
    m2.subtractFrom(sb2)
    sb2.erase()

    m2.rotateY(-135).translate((F1-A2, 0.0, -Fa1/2))
    m1.uniteWith(m2)
    m2.erase()
    m1.translate((0.0, 0.0, -Fa1/2))

    di1 = (Fa1 / (sqrt(2)/2)) / 2
    pl1.rotateY(90).translate((plt/2, 0.0, -Fa1/2))
    pl2.rotateY(90).translate((plt/2, 0.0, -Fa1 - F1 + A2 + di1))
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    pl1.erase()
    pl2.erase()

    sP2 = (-A, 0.0, -Dz/2 - P1 - Fa1/2)
    sP3 = (A1 - A2, 0.0, -Dz/2 - P1 - Fa1)
    sP4 = (-A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2)
    sP5 = (-A, 0.0, -Dz/2 - P1 - Fa1 - (F1 - A2) + di1 - plA/2)
    sPLA = [(0.0, -Fa1/2, -Dz/2 - P1), (-A, -Fa1/2, -Dz/2 - P1)]
    sPLA1 = [(0.0, -Fa1/2, -Dz/2 - P1), (A1, -Fa1/2, -Dz/2 - P1)]

  m1.translate((-A, 0.0, -Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  # For F1 and datum
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  # For F2
  s.setPoint(sP3, (0.0, 0.0, 1.0))
  # For P1_1 and P1_2
  s.setPoint(sP4, (0.0, 0.0, 1.0))
  s.setPoint(sP5, (0.0, 0.0, 1.0))
  s.setPoint((0.0, 0.0, -Dz/2-P1), (0.0, 0.0, 1.0))
  s.setLinearDimension('A', sPLA[0], sPLA[1])
  s.setLinearDimension('A1', sPLA1[0], sPLA1[1])

