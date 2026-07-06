## TRURD
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
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE U = -9|TYPE DU = -10|TYPE R = -11|\
  TYPE DR = -12|")

def TRURD(s, Dn = 150, A = 800.0, A1 = 300.0, A2 = 800.0, A3 = 300.0, 
  P1 = 10.0, TY = -9, ID = "TRURD", **kwargs):
  """
  """
  # Check member profile as standard
  valid_type = [-9, -10, -11, -12]
  if (TY not in valid_type):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type for trunnion profile!")
    print("BI: -9, -10, -11, -12")
    return

  Dz = pipe_size[Dn]
  Qn = trunnion_size[Dn]
  Qz = pipe_size[Qn]

  m1  = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A, 
    Dz = Dz, PW = pipe_wall[Dn])
  
  if (TY == -9):
    ub1 = UBOLT_SHAPE(s, Qn = Qn, Qz = Qz, P1 = P1)
    ub1.rotateY(-90).translate((A1, 0.0, 0.0))

    m1.uniteWith(ub1)
    ub1.erase()

    m1.translate((-A, 0.0, 0.0))

  elif (TY == -10):
    ub1 = UBOLT_SHAPE(s, Qn = Qn, Qz = Qz, P1 = P1)
    ub1.rotateY(-90).translate((A1, 0.0, 0.0))

    m1.uniteWith(ub1)
    ub1.erase()

    m2 = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A2, 
    Dz = Dz, PW = pipe_wall[Dn])

    ub2 = UBOLT_SHAPE(s, Qn = Qn, Qz = Qz, P1 = P1)
    ub2.rotateY(-90).translate((A3, 0.0, 0.0))

    m2.uniteWith(ub2)
    ub2.erase()

    m1.translate((-A, 0.0, 0.0))
    m2.translate((-A2, 0.0, 0.0)).rotateZ(180)
    m1.uniteWith(m2)
    m2.erase()

  elif (TY == -11):
    m1.translate((-A, 0.0, 0.0))

  elif (TY == -12):
    m2 = TRUNNION_SHAPE(s, Dn = Dn, Qn = Qn, Qz = Qz, A = A2, 
    Dz = Dz, PW = pipe_wall[Dn])

    m1.translate((-A, 0.0, 0.0))
    m2.translate((-A2, 0.0, 0.0)).rotateZ(180)
    m1.uniteWith(m2)
    m2.erase()
  
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))
  # For Q1
  s.setPoint((-A, 0.0, 0.0), (0.0, 1.0, 0.0))
  
  if (TY == -9):
    # For UB1
    s.setPoint((A1 - A, 0.0, Qz/2 + 10.0), (0.0, 0.0, 1.0))

  elif (TY == -10):
    # For Q2
    s.setPoint((A2, 0.0, 0.0), (0.0, 1.0, 0.0))
    # For UB1
    s.setPoint((A1 - A, 0.0, Qz/2 + 10.0), (0.0, 0.0, 1.0))
    # For UB2
    s.setPoint((A2 - A3, 0.0, Qz/2 + 10.0), (0.0, 0.0, 1.0))
    s.setLinearDimension('A2', (0.0, Qz/2, 0.0), (A2, Qz/2, 0.0))

  elif (TY == -12):
    # For Q2
    s.setPoint((A2, 0.0, 0.0), (0.0, 1.0, 0.0))
    s.setLinearDimension('A2', (0.0, Qz/2, 0.0), (A2, Qz/2, 0.0))
    
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
