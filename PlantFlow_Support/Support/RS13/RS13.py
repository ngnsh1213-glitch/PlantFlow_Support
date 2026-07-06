## RS13
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
@param(A=LENGTH, TooltipLong="A length")
@param(A1=LENGTH, TooltipLong="A1 length")
@param(F2=LENGTH, TooltipLong="F2 length")
@param(F3=LENGTH, TooltipLong="F3 length")
@param(P1=LENGTH, TooltipLong="P1 length")

def RS13(s, Dn = 100, BI = 110, A = 250.0, A1 = 250.0, 
  F2 = 500.0, F3 = 500.0, P1 = 0.0, ID = "RS13", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [17, 210, 215, 220]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 17, 210, 215, 220")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  Dz = pipe_size[Dn]
  F1 = A + A1

  # Calculate F4, F5 Length
  Fx1 = F2 + P1 - 100.0
  Fx2 = F3 + P1 - 100.0
  F4 = Fx1 / (sqrt(3) / 2)
  F5 = Fx2 / (sqrt(3) / 2)

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = F2)
    m3 = L_SHAPE(s, BF = BF, LE = F3)
    m4 = L_SHAPE(s, BF = BF, LE = F4 + 20.0)  # 20mm for extend
    m4.rotateZ(-90)
    sB1 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_04")
    sB1.rotateY(-60).translate((Fa1, 0.0, 0.0))
    m4.subtractFrom(sB1)
    sB1.erase()
    m5 = L_SHAPE(s, BF = BF, LE = F5 + 20.0)  # 20mm for extend
    sB2 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_04")
    sB2.rotateY(-60).translate((Fa1, 0.0, 0.0))
    m5.subtractFrom(sB2)
    sB2.erase()

    m1.rotateZ(90).rotateX(-90)
    m2.rotateZ(180).rotateY(-90).translate((0.0, F1, 0.0))
    m3.rotateZ(90).rotateY(-90)
    m4.translate((-Fa1, 0.0, -F4))
    m4.rotateY(60).translate((-100.0 + P1, F1, -Fa1))
    m5.translate((-Fa1, 0.0, -F5))
    m5.rotateY(60).translate((-100.0 + P1, 0.0, -Fa1))
    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(m4)
    m1.uniteWith(m5)
    m2.erase()
    m3.erase()
    m4.erase()
    m5.erase()

    sP2 = (-F3 - Dz/2 - P1, -A, 0.0) # this set point is on F3
    sP5 = (-F3 - Dz/2 - P1, -A, -Fa1 - F5*0.5)
    sP2a = (-F2 - Dz/2 - P1, A1, -Fa1) # this set point is on F2
    sP4 = (-F2 - Dz/2 - P1, A1, -Fa1 - F4*0.5)
    sP6 = (- Dz/2 - P1, A1 - Fa1, 0.0)

    sLD = [(-Dz/2 - P1, -A, -Fa1), (-F3 - Dz/2 - P1, -A, -Fa1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    T1 = profile[BI]["T1"]
    T2 = profile[BI]["T2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = F2)
    m3 = C_SHAPE(s, BF = BF, LE = F3)
    m4 = C_SHAPE(s, BF = BF, LE = F4)
    m4.rotateZ(-90)
    sB1 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_04")
    sB1.rotateY(-60).translate((Fa1/2, 0.0, 0.0))
    m4.subtractFrom(sB1)
    sB1.erase()
    sB2 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "TOP_04")
    sB2.rotateY(30).translate((Fa1/2, 0.0, F4))
    m4.subtractFrom(sB2)
    sB2.erase()
    m5 = C_SHAPE(s, BF = BF, LE = F5)
    m5.rotateZ(90)
    sB3 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "BOTTOM_04")
    sB3.rotateY(-60).translate((Fa1/2, 0.0, 0.0))
    m5.subtractFrom(sB3)
    sB3.erase()
    sB4 = TRANS_BOX(s, L = Fa1*2, W = Fa1*2, H = Fa1*2, BASE = "TOP_04")
    sB4.rotateY(30).translate((Fa1/2, 0.0, F5))
    m5.subtractFrom(sB4)
    sB4.erase()

    m1.rotateZ(180).rotateX(-90)
    m2.rotateZ(-90).rotateY(-90).translate((0.0, F1, 0.0))
    m3.rotateZ(90).rotateY(-90)
    m4.translate((-Fa1/2, 0.0, -F4)).rotateY(60)
    m4.translate((-100.0 + P1, F1, -Fa1/2))
    m5.translate((-Fa1/2, 0.0, -F5)).rotateY(60)
    m5.translate((-100.0 + P1, 0.0, -Fa1/2))

    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(m4)
    m1.uniteWith(m5)
    m2.erase()
    m3.erase()
    m4.erase()
    m5.erase()

    sP2 = (-F3 - Dz/2 - P1, -A, 0.0) # this set point is on F3
    sP5 = (-F3 - Dz/2 - P1, -A, -Fa1/2 - F5*0.5)
    sP2a = (-F2 - Dz/2 - P1, A1, -Fa1/2) # this set point is on F2
    sP4 = (-F2 - Dz/2 - P1, A1, -Fa1/2 - F4*0.5)
    sP6 = (-Dz/2 - P1, A1 - Fa1, 0.0)

    sLD = [(-Dz/2 - P1, -A, -Fa1/2), (-F3 - Dz/2 - P1, -A, -Fa1/2)]
  
  m1.translate((-Dz/2 - P1, -A, 0.0))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

  # For F3 and datum
  s.setPoint(sP2, (0.0, 1.0, 0.0))
  # For F2
  s.setPoint(sP2a, (0.0, 1.0, 0.0))
  # For F4
  s.setPoint(sP4, (0.0, 1.0, 0.0))
  # For F5
  s.setPoint(sP5, (0.0, 1.0, 0.0))
  # For F1
  s.setPoint(sP6, (0.0, 1.0, 0.0))

  s.setLinearDimension('A', 
                      (-Dz/2 - P1, 0.0, 0.0), 
                      (-Dz/2 - P1, -A, 0.0))
  s.setLinearDimension('A1', 
                      (-Dz/2 - P1, 0.0, 0.0), 
                      (-Dz/2 - P1, A1, 0.0))
  s.setLinearDimension('F2', 
                      (-Dz/2 - P1, A1, 0.0), 
                      (-F2 - Dz/2 - P1, A1, 0.0))
  s.setLinearDimension('F3', sLD[0], sLD[1])



