## RC9
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

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_RC8_9 as baseplate

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
@param(F2=LENGTH, TooltipLong="F2 length")
@param(F3=LENGTH, TooltipLong="F3 length")
@param(P1=LENGTH, TooltipLong="P1 length")

def RC9(s, Dn = 25, BI = 210, A = 600.0, A1 = 250.0, F2 = 500.0, F3 = 500.0,
  P1 = 0.0, ID = "RC9", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [15, 16, 17, 110, 210, 212, 215, 220, 310, 312, 315]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 15, 16, 17, 110, 210, 212, 215, 220, 310, 312, 315")
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
    
  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2)
    m3 = L_SHAPE(s, BF = BF, LE = F3)
    m1.rotateY(90)
    m2.rotateZ(-90).translate((0.0, 0.0, -F2))
    m3.rotateZ(180).translate((F1, 0.0, -F3))

    pl1.translate((Fa1/2, -Fa1/2, plt/2 - F2))
    pl2.translate((F1 - Fa1/2, -Fa1/2, plt/2 - F3))
    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    m2.erase()
    m3.erase()
    pl1.erase()
    pl2.erase()

    sP2 = (-A, 0.0, -Dz/2 - P1 - F2)
    sP3 = (A1, 0.0, -Dz/2 - P1 - F3)
    sP4 = (-A + Fa1*2, 0.0, -Dz/2 - P1)
    sP5 = (Fa1/2 - A - plA/2, 0.0, -Dz/2 - P1 - F2)
    sP6 = (A1 - Fa1/2 + plA/2, 0.0, -Dz/2 - P1 - F3)
    sPLF2 = [(-A, -Fa1, -Dz/2 - P1), (-A, -Fa1, -Dz/2 - P1 - F2)]
    sPLF3 = [(A1, -Fa1, -Dz/2 - P1), (A1, -Fa1, -Dz/2 - P1 - F3)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = F2)
    m3 = C_SHAPE(s, BF = BF, LE = F3)
    m1.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -Fa1/2))
    m2.rotateZ(-90).translate((-Fa1/2, 0.0, -F2))
    m3.rotateZ(-90).translate((F1 + Fa1/2, 0.0, -F3))

    pl1.translate((-Fa1/2, -Fa2/2, plt/2 - F2))
    pl2.translate((F1 + Fa1/2, -Fa2/2, plt/2 - F3))
    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    m2.erase()
    m3.erase()
    pl1.erase()
    pl2.erase()

    sP2 = (-A - Fa1/2, 0.0, -Dz/2 - P1 - F2)
    sP3 = (A1 + Fa1/2, 0.0, -Dz/2 - P1 - F3)
    sP4 = (-A + Fa1*2, 0.0, -Dz/2 - P1)
    sP5 = (-Fa1/2 - A - plA/2, 0.0, -Dz/2 - P1 - F2)
    sP6 = (A1 + Fa1/2 + plA/2, 0.0, -Dz/2 - P1 - F3)
    sPLF2 = [(-A, -Fa2, -Dz/2 - P1), (-A, -Fa2, -Dz/2 - P1 - F2)]
    sPLF3 = [(A1, -Fa2, -Dz/2 - P1), (A1, -Fa2, -Dz/2 - P1 - F3)]

  elif  (BPn == 3):
    Fa1 = profile[BI]["F"]
    m1 = H_SHAPE(s, BF = BF, LE = F1)
    m2 = H_SHAPE(s, BF = BF, LE = F2)
    m3 = H_SHAPE(s, BF = BF, LE = F3)
    m1.rotateY(90).translate((0.0, 0.0, -Fa1/2))
    m2.translate((-Fa1/2, 0.0, -F2))
    m3.translate((F1 + Fa1/2, 0.0, -F3))

    pl1.translate((-Fa1/2, 0.0, plt/2 - F2))
    pl2.translate((F1 + Fa1/2, 0.0, plt/2 - F3))
    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(pl1)
    m1.uniteWith(pl2)
    m2.erase()
    m3.erase()
    pl1.erase()
    pl2.erase()

    sP2 = (-A - Fa1/2, 0.0, -Dz/2 - P1 - F2)
    sP3 = (A1 + Fa1/2, 0.0, -Dz/2 - P1 - F3)
    sP4 = (-A + Fa1*2, 0.0, -Dz/2 - P1)
    sP5 = (-Fa1/2 - A - plA/2, 0.0, -Dz/2 - P1 - F2)
    sP6 = (A1 + Fa1/2 + plA/2, 0.0, -Dz/2 - P1 - F3)
    sPLF2 = [(-A, -Fa1/2, -Dz/2 - P1), (-A, -Fa1/2, -Dz/2 - P1 - F2)]
    sPLF3 = [(A1, -Fa1/2, -Dz/2 - P1), (A1, -Fa1/2, -Dz/2 - P1 - F3)]

  m1.translate((-A, 0.0, -Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  # For F2 and datum
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  # For F3
  s.setPoint(sP3, (0.0, 0.0, 1.0))
  # For F1
  s.setPoint(sP4, (0.0, 0.0, 1.0))
  # For P1_1 and P1_2
  s.setPoint(sP5, (0.0, 0.0, 1.0))
  s.setPoint(sP6, (0.0, 0.0, 1.0))
  s.setPoint((0.0, 0.0, -Dz/2-P1), (0.0, 0.0, 1.0))
  s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), (-A, 0.0, -Dz/2 - P1))
  s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), (A1, 0.0, -Dz/2 - P1))
  s.setLinearDimension('F2', sPLF2[0], sPLF2[1])
  s.setLinearDimension('F3', sPLF3[0], sPLF3[1])

