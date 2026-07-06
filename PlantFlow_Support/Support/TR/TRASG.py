## TRASG
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

from support_data import pipe_size, pipe_wall, trunnion_size, profile, baseplate

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(A=LENGTH, TooltipLong="A length")
@param(A1=LENGTH, TooltipLong="A1 length")
@param(G1=LENGTH, TooltipLong="G1 length")
@param(G2=LENGTH, TooltipLong="G2 length")
@param(S1=LENGTH, TooltipLong="S1 length")
@param(S2=LENGTH, TooltipLong="S2 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -3|TYPE ST = -4|TYPE G = -5")

def TRASG(s, Dn = 150, A = 800.0, A1 = 300.0, G1 = 120.0, G2 = 150.0, 
  S1 = 130.0, S2 = 170.0, P1 = 10.0, TY = -3, ID = "TRASG", **kwargs):
  """
  """
  # Check member profile as standard
  valid_type = [-3, -4, -5]
  if (TY not in valid_type):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type for trunnion profile!")
    print("BI: -3, -4, -5")
    return

  Dz = pipe_size[Dn]
  Qn = trunnion_size[Dn]
  Qz = pipe_size[Qn]

  m1 = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A, 
    Dz = Dz, PW = pipe_wall[Dn])

  if (TY == -3):
    FaL1 = profile[15]["F"]
    FaC1 = profile[210]["Fa1"]
    g1 = L_SHAPE(s, BF = 5, LE = G2)
    g2 = L_SHAPE(s, BF = 5, LE = G2)
    g1.rotateZ(90).translate((A1 + FaL1/2, G1/2, -Qz/2 - P1))
    g2.rotateZ(180).translate((A1 + FaL1/2, -G1/2, -Qz/2 - P1))

    s1 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s2 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s1.translate((A1 + S1/2, 0.0, -Qz/2 - S2))
    s2.rotateZ(180).translate((A1 - S1/2, 0.0, -Qz/2 - S2))

    m1.uniteWith(g1)
    m1.uniteWith(g2)
    m1.uniteWith(s1)
    m1.uniteWith(s2)
    g1.erase()
    g1.erase()
    s1.erase()
    s2.erase()

  elif (TY == -4):
    FaC1 = profile[210]["Fa1"]
    s1 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s2 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s1.translate((A1 + S1/2, 0.0, -Qz/2 - S2))
    s2.rotateZ(180).translate((A1 - S1/2, 0.0, -Qz/2 - S2))

    m1.uniteWith(s1)
    m1.uniteWith(s2)
    s1.erase()
    s2.erase()

  elif (TY == -5):
    FaL1 = profile[15]["F"]
    g1 = L_SHAPE(s, BF = 5, LE = G2)
    g2 = L_SHAPE(s, BF = 5, LE = G2)
    g1.rotateZ(90).translate((A1 + FaL1/2, G1/2, -Qz/2 - P1))
    g2.rotateZ(180).translate((A1 + FaL1/2, -G1/2, -Qz/2 - P1))

    m1.uniteWith(g1)
    m1.uniteWith(g2)
    g1.erase()
    g2.erase()

  m1.translate((-A, 0.0, 0.0))
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
  # For Q1
  s.setPoint((-A, 0.0, 0.0), (0.0, 1.0, 0.0))
  
  if (TY == -3):
    # For G1
    s.setPoint((A1-A, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G2
    s.setPoint((A1-A, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For S1
    s.setPoint((A1 - A + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S2
    s.setPoint((A1 - A - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

  elif (TY == -4):
    # For S1
    s.setPoint((A1 - A + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S2
    s.setPoint((A1 - A - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

  elif (TY == -5):
    # For G1
    s.setPoint((A1-A, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G2
    s.setPoint((A1-A, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    
  s.setLinearDimension('A', (0.0, Qz/2, 0.0), (-A, Qz/2, 0.0))
  s.setLinearDimension('P1', (A1 - A, 0.0, -Qz/2), (A1 - A, 0.0, -Qz/2 - P1))



def ConvertProfile(BI):
  """
  Convert profile
  """

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  return BF