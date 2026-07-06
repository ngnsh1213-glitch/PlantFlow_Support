## RS4
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
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def RS4(s, Dn = 100, BI = 210, A = 600.0, A1 = 200.0, F2 = 500.0, P1 = 0.0,
  TY = -2, ID = "RS4", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [210, 212, 215, 312, 315]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 210, 212, 215, 312, 315")
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

  Dz = pipe_size[Dn]
  F1 = A + A1

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    T1 = profile[BI]["T"]
    m1 = L_SHAPE(s, BF = BF, LE = F2)
    m2 = L_SHAPE(s, BF = BF, LE = F1)
    if (TY in [-1, -3, -5, -6]):
      m1.rotateZ(90).translate((0.0, 0.0, -F2))
      m2.rotateY(90).translate((0.0, 0.0, Fa1 - F2))
      sP1 = (-Fa1, 0.0, -F2)
      lP1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
      lP2 = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]

    elif (TY in [-2, -4]):
      m1.translate((0.0, 0.0, -F2))
      m2.rotateZ(-90).rotateY(90).translate((0.0, 0.0, Fa1 - F2))
      sP1 = (Fa1, 0.0, -F2)
      lP1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
      lP2 = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]

    m1.uniteWith(m2)
    m2.erase()

    sP2 = (-A1, 0.0, F2 - Fa1 - Dz/2 - P1)
    sP3 = (A, -Fa1, -Dz/2 - P1)
    sPL = [(-A1, Fa1, -Dz/2 - P1 - Fa1), (-A1, Fa1, F2 - Dz/2 - P1 - Fa1)]

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    T1 = profile[BI]["T1"]
    T2 = profile[BI]["T2"]
    m1 = C_SHAPE(s, BF = BF, LE = F2)
    m2 = C_SHAPE(s, BF = BF, LE = F1)
    if (TY in [-1, -3, -5, -6]):
      m1.rotateZ(90).translate((0.0, 0.0, -F2))
      m2.rotateZ(90).rotateY(90).translate((Fa1/2, 0.0, -F2 + Fa1/2))
      sP1 = (-Fa1/2, 0.0, -F2)
      lP1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
      lP2 = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]

      sP2 = (-A1 - Fa1/2, 0.0, F2 - Dz/2 - P1 - Fa1)
      sP3 = (A, Fa2, -Dz/2 - P1)
      sPL = [(-A1, 0.0, -Dz/2 - P1 - Fa1), 
             (-A1, 0.0, F2 - Dz/2 - P1 - Fa1)]

    elif (TY in [-2, -4]):
      m1.rotateZ(90).translate((0.0, 0.0, -F2))
      m2.rotateZ(-90).rotateY(90).translate((-Fa1/2, 0.0, -F2 + Fa1/2))
      sP1 = (-Fa1/2, 0.0, -F2)
      lP1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
      lP2 = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]

      sP2 = (-A1 + Fa1/2, 0.0, F2 - Dz/2 - P1 - Fa1)
      sP3 = (A, -Fa2, -Dz/2 - P1)
      sPL = [(-A1, 0.0, -Dz/2 - P1 - Fa1), 
             (-A1, 0.0, F2 - Dz/2 - P1 - Fa1)]

    m1.uniteWith(m2)
    m2.erase()

  elif  (BPn == 3):
    Fa1 = profile[BI]["F"]
    m1 = H_SHAPE(s, BF = BF, LE = F2)
    m2 = H_SHAPE(s, BF = BF, LE = F1)
    if (TY in [-1, -3, -5, -6]):
      m1.translate((0.0, 0.0, -F2))
      m2.rotateY(90).translate((Fa1/2, 0.0, -F2 + Fa1/2))
      sP1 = (-Fa1/2, 0.0, -F2)
      lP1 = [(0.0, 0.0, -Dz/2 - P1), (-A1, 0.0, -Dz/2 - P1)]
      lP2 = [(0.0, 0.0, -Dz/2 - P1), (A, 0.0, -Dz/2 - P1)]

      sP2 = (-A1 - Fa1/2, 0.0, F2 - Dz/2 - P1 - Fa1)
      sP3 = (A, Fa1/2, -Dz/2 - P1)
      sPL = [(-A1, 0.0, -Dz/2 - P1 - Fa1), 
             (-A1, 0.0, F2 - Dz/2 - P1 - Fa1)]

    elif (TY in [-2, -4]):
      m1.erase()
      m2.erase()
      e1 = ERROR_SHAPE(s, R = 150.0, Mess = "ERROR")
      print("There is no valid shape for type B and D with member profile H.")
      print("Please select another profile.")
      return

    m1.uniteWith(m2)
    m2.erase()
  
  if (BPn == 1):
    m1.translate((-A1, 0.0, F2 - Fa1 - Dz/2 - P1))
  elif (BPn == 2 or BPn == 3):
    if (TY in [-1, -3, -5, -6]):
      m1.translate((-A1 - Fa1/2, 0.0, F2 - Fa1 - Dz/2 - P1))
    elif (TY in [-2, -4]):
      m1.translate((-A1 + Fa1/2, 0.0, F2 - Fa1 - Dz/2 - P1))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

  # For F2 and datum
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  # For F1
  s.setPoint(sP3, (0.0, 0.0, 1.0))
  
  s.setLinearDimension('F2', sPL[0], sPL[1])
  s.setLinearDimension('A', lP2[0], lP2[1])
  s.setLinearDimension('A1', lP1[0], lP1[1])



