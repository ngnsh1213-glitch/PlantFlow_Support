## RS11
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
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def RS11(s, Dn = 15, BI = 15, A = 400.0, A1 = 150.0, P1 = 0.0, 
  TY = -2, ID = "RS11", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [15]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: only 15 for this type of support you can not change profile!")
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
  if (A > 1000.0):
    A = 1000.0
    F1 = A + A1
    print("Amax = 1000mm, please consider your input!")

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m1.rotateZ(-90)

  m1.translate((Dz/2 + P1, 0.0, -A))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))
  s.setPoint((Dz/2 + P1, 0.0, -A), (0.0, 0.0, 1.0))

  s.setLinearDimension('A', (Dz/2 + P1 + Fa1, 0.0, 0.0), (Dz/2 + P1 + Fa1, 0.0, -A))



