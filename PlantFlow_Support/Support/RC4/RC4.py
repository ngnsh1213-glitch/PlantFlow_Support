## RC4
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

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_RC4 as baseplate

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
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def RC4(s, Dn = 100, BI = 110, A = 250.0, A1 = 250.0, 
  F2 = 600.0, P1 = 0.0, TY = -1, ID = "RC4", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [110, 215, 220, 315]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 110, 215, 220, 315")
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
  A2 = 50.0

  Dz = pipe_size[Dn]
  F1 = A + A1

  plA = baseplate[BI]["A"]
  plB = baseplate[BI]["B"]
  plt = baseplate[BI]["t"]
  plh = baseplate[BI]["h"]
  pl1 = BOX(s, L = plA, W = plt, H = plA)
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

  if (TY == -1):
    if (BI != 110):
      pl1.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Only A10 for TYPE-A!")
      return

    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    F2r = F2 - 10
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2r)

    m1.rotateY(90)
    m2.rotateZ(180).translate((F1 - 10.0, 0.0, -F2))

    pl1.translate((F1 - 10.0 - Fa1/2, -Fa1/2, plt/2 - F2))
    m1.uniteWith(m2)
    m1.uniteWith(pl1)
    m2.erase()
    pl1.erase()

    m1.translate((-A1, P1 + Dz/2, 0.0))

    sP2 = (A - 10.0, P1 + Dz/2, -F2)
    sP3 = (-A1, P1 + Dz/2, 0.0)
    sP4 = (A - 10.0 - Fa1/2 + plA/2, P1 + Dz/2, -F2)
    sLD = [(A - 10.0 - Fa1/2, P1 + Dz/2, 0.0), 
           (A - 10.0 - Fa1/2, P1 + Dz/2, -F2)]

    # Set point
    s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

    # For F2 and datumn
    s.setPoint(sP2, (0.0, 1.0, 0.0))
    # For F1
    s.setPoint(sP3, (0.0, 1.0, 0.0))
    # For P1
    s.setPoint(sP4, (0.0, 1.0, 0.0))

    s.setLinearDimension('A', (0.0, P1 + Dz/2, 0.0), (A, P1 + Dz/2, 0.0))
    s.setLinearDimension('F2', sLD[0], sLD[1])

  elif (TY == -2):
    if (BI not in [215, 220, 315]):
      pl1.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Only C15, C20, H15 for TYPE-B!")
      return

    if (BPn == 2):
      Fa1 = profile[BI]["Fa1"]
      Fa2 = profile[BI]["Fa2"]
      Fal1 = profile[17]["F"]
      T1 = profile[BI]["T1"]
      T2 = profile[BI]["T2"]
      F3 = (F1 - 10.0 - Fa1 - A2) / (sqrt(2)/2)
      m1 = C_SHAPE(s, BF = BF, LE = F1)
      m2 = C_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = 7, LE = F3)

      sb1 = TRANS_BOX(s, L = Fal1*2, W = Fal1*2, H = Fal1*2, BASE = "TOP_04")
      sb2 = TRANS_BOX(s, L = Fal1*2, W = Fal1*2, H = Fal1*2, BASE = "BOTTOM_04")
      sb1.rotateY(45).translate((Fal1, 0.0, 0.0))
      sb2.rotateY(-45).translate((Fal1, 0.0, F3))
      m3.subtractFrom(sb1)
      m3.subtractFrom(sb2)
      sb1.erase()
      sb2.erase()

      m1.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -Fa1/2))
      m2.rotateZ(-90).translate((F1 - 10.0 - Fa1/2, 0.0, -F2 - Fa1))
      m3.rotateZ(180).translate((Fal1, 0.0, 0.0))
      m3.rotateY(-45)
      m3.translate((
        (F1 - 10.0 - Fa1 - A2), 
        -(Fa2/2 - Fal1/2), 
        -(F1 - 10.0 - Fa1 - A2)))
      m3.translate((A2, 0.0, -Fa1))
      pl1.translate((F1 - 10.0 - Fa1/2, -Fa2/2, plt/2 - F2 - Fa1))

      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m1.uniteWith(pl1)
      m2.erase()
      m3.erase()
      pl1.erase()

      m1.translate((-A1, 0.0, -Dz/2 - P1))

      sP2 = (A - 10.0 - Fa1/2, 0.0, -F2 - P1 - Dz/2 - Fa1)
      sP3 = (-A1, 0.0, -P1 - Dz/2)
      sP4 = (A - 10.0 - Fa1/2 + plA/2, 0.0, -F2 - P1 - Dz/2 - Fa1)
      sP5 = (A2 - A1, 0.0, -Dz/2 - P1 - Fa1)
      sLD = [(A - 10.0, 0.0, -Fa1 - P1 - Dz/2), 
             (A - 10.0, 0.0, -F2 - P1 - Dz/2 - Fa1)]

      # Set point
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  
      # For F2 and datumn
      s.setPoint(sP2, (0.0, 0.0, 1.0))
      # For F1
      s.setPoint(sP3, (0.0, 0.0, 1.0))
      # For P1
      s.setPoint(sP4, (0.0, 0.0, 1.0))
      # For F3
      s.setPoint(sP5, (0.0, 0.0, 1.0))
  
      s.setLinearDimension('A', (0.0, 0.0, -P1 - Dz/2), (A, 0.0, -P1 - Dz/2))
      s.setLinearDimension('F2', sLD[0], sLD[1])
      
      ptBTop = (A2 - A1, 0.0, -Fa1 - Dz/2 - P1)
      ptBBot = (A - 10.0 - Fa1, 0.0, -(F1 - 10.0 - Fa1 - A2) - Fa1 - Dz/2 - P1)
      s.setLinearDimension('B', ptBTop, ptBBot)

    elif (BPn == 3):
      Fa1 = profile[BI]["F"]
      Fa2 = profile[BI]["F"]
      T1 = profile[BI]["T"]
      Fal1 = profile[17]["F"]
      F3 = (F1 - 10.0 - Fa1 - A2) / (sqrt(2)/2)
      m1 = H_SHAPE(s, BF = BF, LE = F1)
      m2 = H_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = 7, LE = F3)

      sb1 = TRANS_BOX(s, L = Fal1*2, W = Fal1*2, H = Fal1*2, BASE = "TOP_04")
      sb2 = TRANS_BOX(s, L = Fal1*2, W = Fal1*2, H = Fal1*2, BASE = "BOTTOM_04")
      sb1.rotateY(45).translate((Fal1, 0.0, 0.0))
      sb2.rotateY(-45).translate((Fal1, 0.0, F3))
      m3.subtractFrom(sb1)
      m3.subtractFrom(sb2)
      sb1.erase()
      sb2.erase()

      m1.rotateY(90).translate((0.0, 0.0, -Fa1/2))
      m2.translate((F1 - 10.0 - Fa1/2, 0.0, -F2 - Fa1))
      m3.rotateZ(180).translate((Fal1, 0.0, 0.0))
      m3.rotateY(-45)
      m3.translate((
        (F1 - 10.0 - Fa1 - A2), Fal1/2, -(F1 - 10.0 - Fa1 - A2)))
      m3.translate((A2, 0.0, -Fa1))
      pl1.translate((F1 - 10.0 - Fa1/2, 0.0, plt/2 - F2 - Fa1))

      stL = Fa1 - 20.0
      stH = Fa1 - T1*2
      stT = 10.0
      st1 = TRANS_BOX(s, L = stL, W = stH, H = stT, BASE = "BOTTOM_05")
      st2 = TRANS_BOX(s, L = stL, W = stH, H = stT, BASE = "BOTTOM_05")
      st1.translate((F1 - 10.0 - T1/2, 0.0, T1 - Fa1))
      st2.translate((F1 - 10.0 - Fa1 + T1/2, 0.0, T1 - Fa1))

      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m1.uniteWith(pl1)
      m1.uniteWith(st1)
      m1.uniteWith(st2)
      m2.erase()
      m3.erase()
      pl1.erase()
      st1.erase()
      st2.erase()

      m1.translate((-A1, 0.0, -Dz/2 - P1))

      sP2 = (A - 10.0 - Fa1/2, 0.0, -F2 - P1 - Dz/2 - Fa1)
      sP3 = (-A1, 0.0, -P1 - Dz/2)
      sP4 = (A - 10.0 - Fa1/2 + plA/2, 0.0, -F2 - P1 - Dz/2 - Fa1)
      sP5 = (A2 - A1, 0.0, -Dz/2 - P1 - Fa1)
      sLD = [(A - 10.0, 0.0, -Fa1 - P1 - Dz/2), 
             (A - 10.0, 0.0, -F2 - P1 - Dz/2 - Fa1)]

      # Set point
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  
      # For F2 and datumn
      s.setPoint(sP2, (0.0, 0.0, 1.0))
      # For F1
      s.setPoint(sP3, (0.0, 0.0, 1.0))
      # For P1
      s.setPoint(sP4, (0.0, 0.0, 1.0))
      # For F3
      s.setPoint(sP5, (0.0, 0.0, 1.0))
  
      s.setLinearDimension('A', (0.0, 0.0, -P1 - Dz/2), (A, 0.0, -P1 - Dz/2))
      s.setLinearDimension('F2', sLD[0], sLD[1])
      
      ptBTop = (A2 - A1, 0.0, -Fa1 - Dz/2 - P1)
      ptBBot = (A - 10.0 - Fa1, 0.0, -(F1 - 10.0 - Fa1 - A2) - Fa1 - Dz/2 - P1)
      s.setLinearDimension('B', ptBTop, ptBBot)

