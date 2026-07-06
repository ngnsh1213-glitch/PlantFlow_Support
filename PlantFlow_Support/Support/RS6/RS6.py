## RS6
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
@param(Ha=LENGTH, TooltipLong="Ha length")
@param(Hb=LENGTH, TooltipLong="Hb length")
@param(B1=LENGTH, TooltipLong="B1 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|TYPE D = -4|\
  TYPE E = -5")

def RS6(s, Dn = 100, BI = 210, A = 400.0, A1 = 400.0, 
  Ha = 500.0, Hb = 500.0, B1 = 0.0, P1 = 0.0,
  TY = -2, ID = "RS6", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [16, 17, 210, 212, 215, 312, 315]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 16, 17, 210, 212, 215, 312, 315")
    return

  if (TY not in [-1, -2, -3, -4, -5]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: -1, -2, -3, -4, -5")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  if (TY == -4 or TY == -5):
    if (BI in [312, 315, 320]):
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("H-BEAM is not applicable for TYPE E!")
      return

  Dz = pipe_size[Dn]
  F1 = A + A1

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m2 = L_SHAPE(s, BF = BF, LE = Ha)
    m3 = L_SHAPE(s, BF = BF, LE = Hb)

    m1.rotateZ(-90).rotateY(90)
    m2.rotateZ(180).translate((0.0, 0.0, -Fa1))
    m3.rotateZ(-90).translate((F1, 0.0, -Fa1))

    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m2.erase()
    m3.erase()

    sP2 = (-A, 0.0, Ha - Dz/2 - P1 - Fa1)
    sP3 = (A1 + Fa1, 0.0, Hb - Dz/2 - P1 - Fa1)
    sP4 = (-A + Fa1, 0.0, -Dz/2 - P1)
    sPL = [(-A, -Fa1, -Dz/2 - P1 - Fa1), (-A, -Fa1, Ha - Dz/2 - P1 - Fa1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    T1 = profile[BI]["T1"]
    T2 = profile[BI]["T2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m2 = C_SHAPE(s, BF = BF, LE = Ha)
    m3 = C_SHAPE(s, BF = BF, LE = Hb)

    m1.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -Fa1/2))
    m2.rotateZ(-90).translate((-Fa1/2, 0.0, -Fa1))
    m3.rotateZ(-90).translate((F1 + Fa1/2, 0.0, -Fa1))

    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m2.erase()
    m3.erase()

    sP2 = (-A - Fa1/2, 0.0, Ha - Dz/2 - P1 - Fa1)
    sP3 = (A1 + Fa1, 0.0, Hb - Dz/2 - P1 - Fa1)
    sP4 = (-A + Fa1, 0.0, -Dz/2 - P1)
    sPL = [(-A, 0.0, -Dz/2 - P1 - Fa1), (-A, 0.0, Ha - Dz/2 - P1 - Fa1)]

  elif  (BPn == 3):
    Fa1 = profile[BI]["F"]
    m1 = H_SHAPE(s, BF = BF, LE = F1)
    m2 = H_SHAPE(s, BF = BF, LE = Ha)
    m3 = H_SHAPE(s, BF = BF, LE = Hb)

    m1.rotateY(90).translate((0.0, 0.0, -Fa1/2))
    m2.translate((-Fa1/2, 0.0, -Fa1))
    m3.translate((F1 + Fa1/2, 0.0, -Fa1))

    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m2.erase()
    m3.erase()

    sP2 = (-A - Fa1/2, 0.0, Ha - Dz/2 - P1 - Fa1)
    sP3 = (A1 + Fa1, 0.0, Hb - Dz/2 - P1 - Fa1)
    sP4 = (-A + Fa1, 0.0, -Dz/2 - P1)
    sPL = [(-A, 0.0, -Dz/2 - P1 - Fa1), (-A, 0.0, Ha - Dz/2 - P1 - Fa1)]
  
  m1.translate((-A, 0.0, - Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

  # For Ha
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  # For Hb
  s.setPoint(sP3, (0.0, 0.0, 1.0))
  # For F1
  s.setPoint(sP4, (0.0, 0.0, 1.0))
  s.setLinearDimension('A', (0.0, 0.0, -Dz/2 - P1), (-A, 0.0, -Dz/2 - P1))
  s.setLinearDimension('A1', (0.0, 0.0, -Dz/2 - P1), (A1, 0.0, -Dz/2 - P1))
  s.setLinearDimension('Ha', sPL[0], sPL[1])
  s.setLinearDimension('Hb', (A1, 0.0, -Dz/2 - P1 - Fa1), (A1, 0.0, Hb - Dz/2 - P1 - Fa1))


