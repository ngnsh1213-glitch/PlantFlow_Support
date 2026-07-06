## RS14
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
@param(A1=LENGTH, TooltipLong="A1 length")
@param(A2=LENGTH, TooltipLong="A2 length")
@param(P1=LENGTH, TooltipLong="P1 length")

def RS14(s, Dn = 80, BI = 212, A1 = 250.0, A2 = 250.0, P1 = 0.0,
  ID = "RS14", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [15, 16, 17, 210, 212, 215, 312, 315]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 15, 16, 17, 210, 212, 215, 312, 315")
    return

  # Convert profile
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  Dz = pipe_size[Dn]
  F1 = A1 + A2

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m1.rotateZ(-90).rotateY(90).translate((-A1, 0.0, -Dz/2 - P1))
    sPL1 = [(0.0, -Fa1, -Dz/2 - P1), (-A1, -Fa1, -Dz/2 - P1)]
    sPL2 = [(0.0, -Fa1, -Dz/2 - P1), (A2, -Fa1, -Dz/2 - P1)]
    sP2 = (-A1, 0.0, -Dz/2 - P1)

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m1.rotateZ(-90).rotateY(90).translate((-A1, 0.0, -Dz/2 - Fa1/2 - P1))
    sPL1 = [(0.0, -Fa2, -Dz/2 - P1), (-A1, -Fa2, -Dz/2 - P1)]
    sPL2 = [(0.0, -Fa2, -Dz/2 - P1), (A2, -Fa2, -Dz/2 - P1)]
    sP2 = (-A1, 0.0, -Dz/2 - P1)

  elif (BPn == 3):
    Fa1 = profile[BI]["F"]
    m1 = H_SHAPE(s, BF = BF, LE = F1)
    m1.rotateY(90).translate((-A1, 0.0, -Dz/2 - Fa1/2 - P1))
    sPL1 = [(0.0, -Fa1/2, -Dz/2 - P1), (-A1, -Fa1/2, -Dz/2 - P1)]
    sPL2 = [(0.0, -Fa1/2, -Dz/2 - P1), (A2, -Fa1/2, -Dz/2 - P1)]
    sP2 = (-A1, 0.0, -Dz/2 - P1)

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  s.setPoint(sP2, (0.0, 0.0, 1.0))
  s.setLinearDimension('A1', sPL1[0], sPL1[1])
  s.setLinearDimension('A2', sPL2[0], sPL2[1])



