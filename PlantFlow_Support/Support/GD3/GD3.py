## GD3
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

from support_data import pipe_size_JIS as pipe_size, profile, support_list_GD3 as support_list

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member M profile")
@param(F1=LENGTH, TooltipLong="F1 length")

def GD3(s, Dn = 150, BI = 212, F1 = 500.0, ID = "GD3", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [210, 212, 215, 315, 320]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 210, 212, 215, 315, 320")
    return


  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  BIl = 17

  C = support_list[Dn]["C"]
  F1l = F1 + C + 85.0
  if (BPn == 2):
    m11 = C_SHAPE(s, BF = BF, LE = F1l)
    m12 = C_SHAPE(s, BF = BF, LE = F1l)
    F1w = profile[BI]["Fa2"]
    F1h = profile[BI]["Fa1"]
    m11.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -F1h/2))
    m12.rotateZ(90).rotateY(90).translate((0.0, 2*C, -F1h/2))

    sP2 = (-F1, -C, -F1h/2)
    sP3 = (-F1, C, -F1h/2)
    sP4 = (-C - 75.0, 0.0, 0.0)
    sP5 = (C + 75.0, 0.0, 0.0)

  elif (BPn == 3):
    m11 = H_SHAPE(s, BF = BF, LE = F1l)
    m12 = H_SHAPE(s, BF = BF, LE = F1l)
    F1w = profile[BI]["F"]
    F1h = profile[BI]["F"]
    m11.rotateY(90).translate((0.0, 0.0 - F1w/2, -F1h/2))
    m12.rotateY(90).translate((0.0, 2*C + F1w/2, -F1h/2))

    sP2 = (-F1, -C - F1w/2, -F1h/2)
    sP3 = (-F1, C, 0.0)
    sP4 = (-C - 75.0, 0.0, 0.0)
    sP5 = (C + 75.0, 0.0, 0.0)

  F2l = 2 * C + 2 * F1w
  BFl = ConvertProfile(BIl)
  m21 = L_SHAPE(s, BF = BFl, LE = F2l)
  m22 = L_SHAPE(s, BF = BFl, LE = F2l)
  m21.rotateZ(90).rotateX(90).translate((F1 - C, F2l - F1w, 0.0))
  m22.rotateX(90).translate((F1 + C, F2l - F1w, 0.0))

  m11.uniteWith(m12)
  m11.uniteWith(m21)
  m11.uniteWith(m22)
  m12.erase()
  m21.erase()
  m22.erase()

  m11.translate((-F1, -C, 0.0))

  # # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

  # For F2 and datum
  s.setPoint(sP2, (0.0, 1.0, 0.0))
  # For F1
  s.setPoint(sP3, (0.0, 1.0, 0.0))
  # For F3
  s.setPoint(sP4, (0.0, 1.0, 0.0))
  # For F4
  s.setPoint(sP5, (0.0, 1.0, 0.0))
  s.setLinearDimension('F1', (0.0, C + F1w, 0.0), (-F1, C + F1w, 0.0))

# =====================================================================

def ConvertProfile(BI):
  """
  Convert profile
  """

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  return BF