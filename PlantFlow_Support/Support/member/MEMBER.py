## MEMBER
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

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="1")
@group("MainDimensions")
@param(BI=LENGTH, TooltipLong="Beam profile")
@param(LE=LENGTH, TooltipLong="Horizontal beam length", Ask4Dist=True)

def MEMBER(s, BI = 315, LE = 500.0, ID = "MEMBER", **kwargs):
  """
  Inverted L port support frame
  BI = BPn + BF (in string type)
  BPn = 1: profile L
  BPn = 2: profile C
  LE = Length of Beam
  """
  # convert profile 
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  # Create beam
  if (BPn == 1):
    # BP = "L"
    F1 = L_SHAPE(s, BF = BF, LE = LE)
  elif (BPn == 2):
    # BP = "C"
    F1 = C_SHAPE(s, BF = BF, LE = LE)
  elif (BPn == 3):
    # BP = "H"
    F1 = H_SHAPE(s, BF = BF, LE = LE)
  elif (BPn == 4):
    # BP = "F"
    F1 = FLAT_SHAPE(s, BF = BF, LE = LE)

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, -1.0, 0.0))
  s.setLinearDimension('LE', (0.0, 0.0, 0.0), (0.0, 0.0, LE))

