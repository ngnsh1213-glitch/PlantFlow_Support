## M1
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
@param(BI=LENGTH, TooltipLong="Member profile")
@param(LE=LENGTH, TooltipLong="Member length", Ask4Dist=True)

def M1(s, BI = 310, LE = 500.0, ID = "M1", **kwargs):
  """
  Inverted L port support frame
  BI = BPn + BF (in string type)
  BPn = 1: profile L
  BPn = 2: profile C
  LE = Length of Beam
  """
  valid_member = [
  12, 15, 16, 17, 19, 110, 113, 115, 117, 120, 125, 
  210, 212, 215, 220, 225, 230,
  310, 312, 315, 317, 320, 325, 330,
  425, 450, 475, 4100]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile as in the pipe support document!")
    print("L: 12, 15, 16, 17, 19, 110, 113, 115, 117, 120, 125")
    print("C: 210, 212, 215, 220, 225, 230")
    print("H: 310, 312, 315, 317, 320, 325, 330")
    print("F: 25, 450, 475, 4100")
    return

  # convert profile sr
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


def GetProfileDimension(BI):
  profile = {
    12:  {"F": 20.0,  "T": 3.0},
    15:  {"F": 50.0,  "T": 6.0},
    16:  {"F": 65.0,  "T": 6.0},
    17:  {"F": 75.0,  "T": 9.0},
    19:  {"F": 90.0,  "T": 10.0},
    110: {"F": 100.0, "T": 10.0},
    113: {"F": 130.0, "T": 12.0},
    115: {"F": 150.0, "T": 15.0},
    117: {"F": 175.0, "T": 15.0},
    120: {"F": 200.0, "T": 20.0},
    125: {"F": 250.0, "T": 25.0},
    210: {"Fa1": 100.0, "Fa2": 50.0, "T1": 5.0, "T2": 7.5},
    212: {"Fa1": 125.0, "Fa2": 65.0, "T1": 6.0, "T2": 8.0},
    215: {"Fa1": 150.0, "Fa2": 75.0, "T1": 6.5, "T2": 10.0},
    220: {"Fa1": 200.0, "Fa2": 90.0, "T1": 8.0, "T2": 13.5},
    225: {"Fa1": 250.0, "Fa2": 90.0, "T1": 9.0, "T2": 13.0},
    230: {"Fa1": 300.0, "Fa2": 90.0, "T1": 9.0, "T2": 13.0},
    310: {"F": 100.0, "W": 6.0, "T": 8.0},
    312: {"F": 125.0, "W": 6.5, "T": 9.0},
    315: {"F": 150.0, "W": 7.0, "T": 10.0},
    317: {"F": 175.0, "W": 7.5, "T": 11.0},
    320: {"F": 200.0, "W": 8.0, "T": 12.0},
    325: {"F": 250.0, "W": 9.0, "T": 14.0},
    330: {"F": 300.0, "W": 9.0, "T": 14.0},
    425: {"W": 25.0, "T": 6.0},
    450: {"W": 50.0, "T": 6.0},
    475: {"W": 75.0, "T": 9.0},
    4100: {"W": 100.0, "T": 9.0},
  }
  return profile[BI]