## TRDASG
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
@param(A2=LENGTH, TooltipLong="A2 length")
@param(A3=LENGTH, TooltipLong="A3 length")
@param(G1=LENGTH, TooltipLong="G1 length")
@param(G2=LENGTH, TooltipLong="G2 length")
@param(S1=LENGTH, TooltipLong="S1 length")
@param(S2=LENGTH, TooltipLong="S2 length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE DA = -6|TYPE DST = -7|TYPE DG = -8")

def TRDASG(s, Dn = 150, A = 800.0, A1 = 300.0, A2 = 800.0, A3 = 300.0, 
  G1 = 120.0, G2 = 150.0, S1 = 130.0, S2 = 170.0, P1 = 10.0, 
  TY = -6, ID = "TRDASG", **kwargs):
  """
  """
  # Check member profile as standard
  valid_type = [-6, -7, -8]
  if (TY not in valid_type):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type for trunnion profile!")
    print("BI: -6, -7, -8")
    return

  Dz = pipe_size[Dn]
  Qn = trunnion_size[Dn]
  Qz = pipe_size[Qn]

  m1 = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A, 
    Dz = Dz, PW = pipe_wall[Dn])

  m2 = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A2, 
    Dz = Dz, PW = pipe_wall[Dn])
  
  if (TY == -6):
    FaL1 = profile[15]["F"]
    FaC1 = profile[210]["Fa1"]
    g11 = L_SHAPE(s, BF = 5, LE = G2)
    g12 = L_SHAPE(s, BF = 5, LE = G2)
    g21 = L_SHAPE(s, BF = 5, LE = G2)
    g22 = L_SHAPE(s, BF = 5, LE = G2)
    g11.rotateZ(90).translate((A1 + FaL1/2, G1/2, -Qz/2 - P1))
    g12.rotateZ(180).translate((A1 + FaL1/2, -G1/2, -Qz/2 - P1))
    g21.rotateZ(90).translate((A3 + FaL1/2, G1/2, -Qz/2 - P1))
    g22.rotateZ(180).translate((A3 + FaL1/2, -G1/2, -Qz/2 - P1))

    s11 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s12 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s21 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s22 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s11.translate((A1 + S1/2, 0.0, -Qz/2 - S2))
    s12.rotateZ(180).translate((A1 - S1/2, 0.0, -Qz/2 - S2))
    s21.translate((A3 + S1/2, 0.0, -Qz/2 - S2))
    s22.rotateZ(180).translate((A3 - S1/2, 0.0, -Qz/2 - S2))

    m1.uniteWith(g11)
    m1.uniteWith(g12)
    m1.uniteWith(s11)
    m1.uniteWith(s12)
    g11.erase()
    g12.erase()
    s11.erase()
    s12.erase()

    m2.uniteWith(g21)
    m2.uniteWith(g22)
    m2.uniteWith(s21)
    m2.uniteWith(s22)
    g21.erase()
    g22.erase()
    s21.erase()
    s22.erase()

  elif (TY == -7):
    FaC1 = profile[210]["Fa1"]
    s11 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s12 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s21 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s22 = C_SHAPE(s, BF = 10, LE = S2 + Qz/2)
    s11.translate((A1 + S1/2, 0.0, -Qz/2 - S2))
    s12.rotateZ(180).translate((A1 - S1/2, 0.0, -Qz/2 - S2))
    s21.translate((A3 + S1/2, 0.0, -Qz/2 - S2))
    s22.rotateZ(180).translate((A3 - S1/2, 0.0, -Qz/2 - S2))

    m1.uniteWith(s11)
    m1.uniteWith(s12)
    s11.erase()
    s12.erase()

    m2.uniteWith(s21)
    m2.uniteWith(s22)
    s21.erase()
    s22.erase()

  elif (TY == -8):
    FaL1 = profile[15]["F"]
    g11 = L_SHAPE(s, BF = 5, LE = G2)
    g12 = L_SHAPE(s, BF = 5, LE = G2)
    g21 = L_SHAPE(s, BF = 5, LE = G2)
    g22 = L_SHAPE(s, BF = 5, LE = G2)
    g11.rotateZ(90).translate((A1 + FaL1/2, G1/2, -Qz/2 - P1))
    g12.rotateZ(180).translate((A1 + FaL1/2, -G1/2, -Qz/2 - P1))
    g21.rotateZ(90).translate((A3 + FaL1/2, G1/2, -Qz/2 - P1))
    g22.rotateZ(180).translate((A3 + FaL1/2, -G1/2, -Qz/2 - P1))

    m1.uniteWith(g11)
    m1.uniteWith(g12)
    g11.erase()
    g12.erase()

    m2.uniteWith(g21)
    m2.uniteWith(g22)
    g21.erase()
    g22.erase()

  m1.translate((-A, 0.0, 0.0))
  m2.translate((-A2, 0.0, 0.0)).rotateZ(180)
  m1.uniteWith(m2)
  m2.erase()
  
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
  # For Q1
  s.setPoint((-A, 0.0, 0.0), (0.0, 1.0, 0.0))
  # For Q2
  s.setPoint((A2, 0.0, 0.0), (0.0, 1.0, 0.0))
  
  if (TY == -6):
    # For G11
    s.setPoint((A1 - A, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G12
    s.setPoint((A1 - A, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For S11
    s.setPoint((A1 - A + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S12
    s.setPoint((A1 - A - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

    # For G21
    s.setPoint((A2 - A3, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G22
    s.setPoint((A2 - A3, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For S21
    s.setPoint((A2 - A3 + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S22
    s.setPoint((A2 - A3 - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

  elif (TY == -7):
    # For S11
    s.setPoint((A1 - A + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S12
    s.setPoint((A1 - A - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

    # For S21
    s.setPoint((A2 - A3 + S1/2, FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))
    # For S22
    s.setPoint((A2 - A3 - S1/2, -FaC1/2, -Qz/2 - S2), (0.0, 0.0, 1.0))

  elif (TY == -8):
    # For G11
    s.setPoint((A1 - A, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G12
    s.setPoint((A1 - A, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))

    # For G21
    s.setPoint((A2 - A3, -Qz/2 - FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    # For G22
    s.setPoint((A2 - A3, Qz/2 + FaL1, G2 - Qz/2 - P1), (0.0, 0.0, 1.0))
    
  s.setLinearDimension('A', (0.0, Qz/2, 0.0), (-A, Qz/2, 0.0))
  s.setLinearDimension('A2', (0.0, Qz/2, 0.0), (A2, Qz/2, 0.0))
  s.setLinearDimension('P1', (A1 - A, 0.0, -Qz/2), (A1 - A, 0.0, -Qz/2 - P1))


def ConvertProfile(BI):
  """
  Convert profile
  """

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  return BF

