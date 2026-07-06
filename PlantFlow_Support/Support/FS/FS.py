## FS
## Developed by Noh Seunghwan
## Last Modified: Apr 29, 2026
## V:\4_부서관리\9_배관팀\03 설계자료\03_02 Support\PIPE SUPPORT STANDARD_R0_260311
## Linked Backend: [[scan_HANTEC_cs_20260426_144159|HANTEC]], [[scan_BOMs_cs_20260426_144100|BOMs]], [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
##--------------------------------
from varmain.primitiv import * 
from varmain.var_basic import * 
from varmain.custom import *

import os
import sys
directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_FS as baseplate

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
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE I = -1|TYPE L = -2|TYPE M = -3|\
  TYPE T = -4|TYPE V = -5")
@param(SM=LENGTH, TooltipLong="Supporting method S = -1, C = -2")
@param(SD=LENGTH, TooltipLong="Supporting dir U = -1, D = -2, S = -3")

def FS(s, Dn = 25, BI = 17, A = 250, A1 = 250.0, F2 = 500.0,
  P1 = 0.0, TY = -1, SM = -1, SD = -1, ID = "FS", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [17]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 17")
    return

  if (SM not in [-1, -2]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Supporting method S = -1, C = -2 only")
    return

  if (SD not in [-1, -2, -3]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Supporting direction U = -1, D = -2, S = -3 only")
    return


  # Convert profile
  BPs = str(BI)
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
    
  def _draw_type_1():
    pl2.erase()
    if (SD == -1):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m1.rotateZ(180)
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((-Fa1/2, -Fa1/2, plt/2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-Dz/2 - P1, 0.0, -A))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # For F1
      s.setPoint((-Dz/2 - P1, 0.0, -A), (0.0, 0.0, 1.0))
      if (SM == -2):
        # For P1
        s.setPoint((-Dz/2 - P1 - Fa1/2 - plA/2, 0.0, -A), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (-Dz/2 - P1, 0.0, 0.0), 
                                (-Dz/2 - P1, 0.0, -A))
      s.setLinearDimension('A1', (-Dz/2 - P1, 0.0, 0.0), 
                                 (-Dz/2 - P1, 0.0, A1))
    elif (SD == -2):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m1.rotateZ(180)
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((-Fa1/2, -Fa1/2,  F1 - plt/2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-Dz/2 - P1, 0.0, -A1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      s.setPoint((-Dz/2 - P1, 0.0, A), (0.0, 0.0, 1.0))
      if (SM == -2):
        s.setPoint((-Dz/2 - P1 - Fa1/2 - plA/2, 0.0, A), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (-Dz/2 - P1, 0.0, 0.0), 
                                (-Dz/2 - P1, 0.0, A))
      s.setLinearDimension('A1', (-Dz/2 - P1, 0.0, 0.0), 
                                 (-Dz/2 - P1, 0.0, -A1))
    elif (SD == -3):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m1.rotateZ(-90).rotateY(90)
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.rotateY(90).translate((plt/2, -Fa1/2, -Fa1/2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      s.setPoint((-A, 0.0, -Dz/2 - P1), (0.0, 1.0, 0.0))
      if (SM == -2):
        s.setPoint((-A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), 
                                 (A1, 0.0, -Dz/2 - P1))
  def _draw_type_2():
    pl2.erase()
    if (SD == -1):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2)
      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(-90).translate((0.0, 0.0, -F2))
      m1.uniteWith(m2)
      m2.erase()
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((Fa1/2, -Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # For F2
      s.setPoint((-A, 0.0, -Dz/2 - P1 - F2), (0.0, 0.0, 1.0))
      # For F1
      s.setPoint((A1, -Fa1, -Dz/2 - P1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # For P1
        s.setPoint((-A + Fa1/2 - plA/2, 0.0, -F2 - Dz/2 - P1), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), 
                                (A1, 0.0, -Dz/2 - P1))
      s.setLinearDimension('F2', (-A, -Fa1, -Dz/2 - P1), 
                                (-A, -Fa1, -Dz/2 - P1 - F2))
    elif (SD == -2):
      Fa1 = profile[BI]["F"]
      T1 = profile[BI]["T"]
      m1 = L_SHAPE(s, BF = BF, LE = F1 + T1 - Fa1)
      m2 = L_SHAPE(s, BF = BF, LE = F2 + T1 - Fa1)
      m1.rotateZ(-90).rotateY(90).translate((-T1, 0.0, 0.0))
      m2.rotateZ(180).translate((0.0, 0.0, -T1))
      m1.uniteWith(m2)
      m2.erase()
      tpl = BOX(s, L = T1, W = Fa1, H = Fa1)
      tpl.translate((-Fa1/2, -T1/2, -Fa1/2))
      m1.uniteWith(tpl)
      tpl.erase()
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((-Fa1/2, -Fa1/2, F2 - Fa1 - plt/2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((Fa1 - A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      s.setPoint((-A + Fa1, 0.0, F2 - Fa1 - Dz/2 - P1), (0.0, 0.0, 1.0))
      s.setPoint((A1, -Fa1, -Dz/2 - P1), (0.0, 0.0, 1.0))
      if (SM == -2):
        s.setPoint((-A + Fa1/2 - plA/2, 0.0, F2 - Fa1 - Dz/2 - P1), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), 
                                (A1, 0.0, -Dz/2 - P1))
      s.setLinearDimension('F2', (-A, -Fa1, -Dz/2 - P1 - Fa1), 
                                (-A, -Fa1, F2 - Fa1 - Dz/2 - P1))
    elif (SD == -3):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2)
      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(-90).translate((0.0, 0.0, -F2))
      m1.uniteWith(m2)
      m2.erase()
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((Fa1/2, -Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-A, -Dz/2 - P1, 0.0))
      # m1.rotateX(90).translate((-A, Dz/2 + P1, 0.0))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
      # s.setPoint((-A, Dz/2 + P1 + F2, 0.0), (0.0, 0.0, 1.0))
      s.setPoint((-A, -Dz/2 - P1, -F2), (0.0, 0.0, 1.0))
      s.setPoint((A1, -Dz/2 - P1 - Fa1, 0.0), (0.0, 0.0, 1.0))
      if (SM == -2):
        s.setPoint((-A + Fa1/2 - plA/2, 0.0, -F2), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, -Dz/2 - P1, 0.0), 
                                (-A, -Dz/2 - P1, 0.0))
      s.setLinearDimension('A1', (0.0, -Dz/2 - P1, 0.0), 
                                (A1, -Dz/2 - P1, 0.0))
      s.setLinearDimension('F2', (Fa1 - A, -Dz/2 - P1, 0.0), 
                                (Fa1 - A, -Dz/2 - P1, -F2))
  def _draw_type_3():
    if (SD == -1):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = BF, LE = F2)
      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(-90).translate((0.0, 0.0, -F2))
      m3.rotateZ(180).translate((F1, 0.0, -F2))
      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m2.erase()
      m3.erase()
      if (SM == -1):
        pl1.erase()
        pl2.erase()
      elif (SM == -2):
        pl1.translate((Fa1/2, -Fa1/2, plt/2 - F2))
        pl2.translate((F1 - Fa1/2, -Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        m1.uniteWith(pl2)
        pl1.erase()
        pl2.erase()
      m1.translate((-A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # F2
      s.setPoint((-A, 0.0, -Dz/2 - P1 - F2), (0.0, 0.0, 1.0))
      # F3
      s.setPoint((A1, 0.0, -Dz/2 - P1 - F2), (0.0, 0.0, 1.0))
      # F1
      s.setPoint((Fa1 - A, 0.0, -Dz/2 - P1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # P1
        s.setPoint((Fa1/2 - A - plA/2, 0.0, -Dz/2 - P1 - F2), 
                  (0.0, 0.0, 1.0))
        # P2
        s.setPoint((A1 - Fa1/2 + plA/2, 0.0, -Dz/2 - P1 - F2), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1',(0.0, 0.0, -Dz/2 - P1), 
                                (A1, 0.0, -Dz/2 - P1))
      s.setLinearDimension('F2', (-A, -Fa1, -Dz/2 - P1), 
                                (-A, -Fa1, -Dz/2 - P1 - F2))
    elif (SD == -2):
      Fa1 = profile[BI]["F"]
      T1 = profile[BI]["T"]
      m1 = L_SHAPE(s, BF = BF, LE = F1 - 2*Fa1 + 2*T1)
      m2 = L_SHAPE(s, BF = BF, LE = F2 - Fa1 + T1)
      m3 = L_SHAPE(s, BF = BF, LE = F2 - Fa1 + T1)
      m1.rotateZ(-90).rotateY(90).translate((-T1, 0.0, 0.0))
      m2.rotateZ(180).translate((0.0, 0.0, -T1))
      m3.rotateZ(-90).translate((F1 - 2*Fa1, 0.0, -T1))
      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m2.erase()
      m3.erase()
      tpl1 = BOX(s, L = T1, W = Fa1, H = Fa1)
      tpl2 = BOX(s, L = T1, W = Fa1, H = Fa1)
      tpl1.translate((-Fa1/2, -T1/2, -Fa1/2))
      tpl2.translate((F1 - 2*Fa1 + Fa1/2, -T1/2, -Fa1/2))
      m1.uniteWith(tpl1)
      m1.uniteWith(tpl2)
      tpl1.erase()
      tpl2.erase()
      if (SM == -1):
        pl1.erase()
        pl2.erase()
      elif (SM == -2):
        pl1.translate((-Fa1/2, -Fa1/2, F2 - Fa1 - plt/2))
        pl2.translate((F1 - 2*Fa1 + Fa1/2, -Fa1/2, F2 - Fa1 - plt/2))
        m1.uniteWith(pl1)
        m1.uniteWith(pl2)
        pl1.erase()
        pl2.erase()
      m1.translate((Fa1 - A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # F2
      s.setPoint((-A + Fa1, 0.0, -Dz/2 - P1 + F2 - Fa1), (0.0, 0.0, 1.0))
      # F3
      s.setPoint((A1, 0.0, -Dz/2 - P1 + F2 - Fa1), (0.0, 0.0, 1.0))
      # F1
      s.setPoint((2*Fa1 - A, 0.0, -Dz/2 - P1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # P1
        s.setPoint((Fa1/2 - A - plA/2, 0.0, -Dz/2 - P1 + F2 - Fa1), 
                  (0.0, 0.0, 1.0))
        # P2
        s.setPoint((A1 - Fa1/2 + plA/2, 0.0, -Dz/2 - P1 + F2 - Fa1), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), 
                                (A1, 0.0, -Dz/2 - P1))
      s.setLinearDimension('F2', (-A, -Fa1, -Dz/2 - P1 - Fa1), 
                                (-A, -Fa1, -Dz/2 - P1 + F2 - Fa1))
    elif (SD == -3):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2)
      m3 = L_SHAPE(s, BF = BF, LE = F2)
      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(-90).translate((0.0, 0.0, -F2))
      m3.rotateZ(180).translate((F1, 0.0, -F2))
      m1.uniteWith(m2)
      m1.uniteWith(m3)
      m2.erase()
      m3.erase()
      if (SM == -1):
        pl1.erase()
        pl2.erase()
      elif (SM == -2):
        pl1.translate((Fa1/2, -Fa1/2, plt/2 - F2))
        pl2.translate((F1 - Fa1/2, -Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        m1.uniteWith(pl2)
        pl1.erase()
        pl2.erase()
      m1.translate((-A, -Dz/2 - P1, 0.0))
      # m1.rotateX(90).translate((-A, Dz/2 + P1, 0.0))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
      # TO HERE
      # F2
      s.setPoint((-A, -Dz/2 - P1, -F2), (0.0, 0.0, 1.0))
      # F3
      s.setPoint((A1, -Dz/2 - P1, -F2), (0.0, 0.0, 1.0))
      # F1
      s.setPoint((Fa1 - A, -Dz/2 - P1, 0.0), (0.0, 0.0, 1.0))
      if (SM == -2):
        # P1
        s.setPoint((Fa1/2 - A - plA/2, -Dz/2 - P1, -F2), 
                  (0.0, 0.0, 1.0))
        # P2
        s.setPoint((A1 - Fa1/2 + plA/2, -Dz/2 - P1, -F2), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, -Dz/2 - P1, 0.0), 
                                (-A, -Dz/2 - P1, 0.0))
      s.setLinearDimension('A1', (0.0, -Dz/2 - P1, 0.0), 
                                (A1, -Dz/2 - P1, 0.0))
      s.setLinearDimension('F2', (-A + Fa1, -Dz/2 - P1, 0.0), 
                                (-A + Fa1, -Dz/2 - P1, -F2))
  def _draw_type_4():
    pl2.erase()
    if (SD not in [-1, -3]):
      pl1.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("SD = -2 is invalid for T type!")
      return
    if (SD == -1):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2 - 10.0)
      m1.rotateZ(-90).rotateY(90)
      m2.translate((A - Fa1/2, 0.0, -F2))
      m1.uniteWith(m2)
      m2.erase()
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((A, Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # For F2
      s.setPoint((-Fa1/2, 0.0, -Dz/2 - P1 - F2), (0.0, 0.0, 1.0))
      # For F1
      s.setPoint((-A, -Fa1, -Dz/2 - P1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # For P1
        s.setPoint((-plA/2, 0.0, -F2 - Dz/2 - P1), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), 
                                (-A, 0.0, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), 
                                (A1, 0.0, -Dz/2 - P1))
      s.setLinearDimension('F2', (0.0, -Fa1, -Dz/2 - P1), 
                                (0.0, -Fa1, -Dz/2 - P1 - F2))
    elif (SD == -3):
      Fa1 = profile[BI]["F"]
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2 - 10.0)
      m1.rotateZ(-90).rotateY(90)
      m2.translate((A - Fa1/2, 0.0, -F2))
      m1.uniteWith(m2)
      m2.erase()
      if (SM == -1):
        pl1.erase()
      elif (SM == -2):
        pl1.translate((A, Fa1/2, plt/2 - F2))
        m1.uniteWith(pl1)
        pl1.erase()
      m1.translate((-A, -Dz/2 - P1, 0.0))
      # m1.rotateX(-90).translate((-A, -Dz/2 - P1, 0.0))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
      # For F2
      s.setPoint((-Fa1/2, -Dz/2 - P1, -F2), (0.0, 0.0, 1.0))
      # For F1
      s.setPoint((-A, -Dz/2 - P1, -Fa1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # For P1
        s.setPoint((-plA/2, -Dz/2 - P1, -F2), 
                  (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, -Dz/2 - P1, 0.0), 
                                (-A, -Dz/2 - P1, 0.0))
      s.setLinearDimension('A1', (0.0, -Dz/2 - P1, 0.0), 
                                (A1, -Dz/2 - P1, 0.0))
      s.setLinearDimension('F2', (Fa1/2, -Dz/2 - P1, 0.0), 
                                (Fa1/2, -Dz/2 - P1, -F2))
  def _draw_type_5():
    if (SD != -1):
      pl1.erase()
      pl2.erase()
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("SD = -2, -3 is invalid for V type!")
      return
    if (SD == -1):
      Fa1 = profile[BI]["F"]
      F2v = (F1 - 200.0) / (sqrt(3) / 2)
      m1 = L_SHAPE(s, BF = BF, LE = F1)
      m2 = L_SHAPE(s, BF = BF, LE = F2v)
      m1.rotateZ(-90).rotateY(90)
      m2.rotateZ(-90)
      sb1 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_06")
      sb2 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_06")
      sb1.rotateY(30).translate((0.0, 0.0, F2v))
      sb2.rotateY(30)
      m2.subtractFrom(sb1)
      m2.subtractFrom(sb2)
      sb1.erase()
      sb2.erase()
      m2.rotateY(-120).translate((F1 - 200.0, 0.0, 0.0))
      m1.uniteWith(m2)
      m2.erase()
      dt1 = (F1 - 200.0) * (1 / sqrt(3))
      if (SM == -1):
        pl1.erase()
        pl2.erase()
      elif (SM == -2):
        pl1.rotateY(90).translate((plt/2, -Fa1/2, -Fa1/2))
        pl2.rotateY(90).translate((plt/2, -Fa1/2, Fa1/2 - dt1))
        m1.uniteWith(pl1)
        m1.uniteWith(pl2)
        pl1.erase()
        pl2.erase()
      m1.translate((-A, 0.0, -Dz/2 - P1))
      s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
      # For F1
      s.setPoint((-A, 0.0, -Dz/2 - P1), (0.0, 0.0, 1.0))
      # For F2
      s.setPoint((-A, -Fa1, -Dz/2 - P1 - dt1), (0.0, 0.0, 1.0))
      if (SM == -2):
        # For P1_1
        s.setPoint((-A, 0.0, -Dz/2 - P1 - Fa1/2 + plA/2), (0.0, 0.0, 1.0))
        # For P1_2
        s.setPoint((-A, 0.0, -Dz/2 - P1 - dt1 + Fa1/2 - plA/2), (0.0, 0.0, 1.0))
      s.setLinearDimension('A', (0.0, -Fa1, -Dz/2 - P1), 
                                (-A, -Fa1, -Dz/2 - P1))
      s.setLinearDimension('A1', (0.0, -Fa1, -Dz/2 - P1), 
                                (A1, -Fa1, -Dz/2 - P1))
# =====================================================================

  # --------------------------------------------------------
  # Type Routing
  # --------------------------------------------------------
  if TY == -1:
    _draw_type_1()
  elif TY == -2:
    _draw_type_2()
  elif TY == -3:
    _draw_type_3()
  elif TY == -4:
    _draw_type_4()
  elif TY == -5:
    _draw_type_5()
