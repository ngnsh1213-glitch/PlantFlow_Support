## TRPT
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
@param(TY=LENGTH, TooltipLong="|TYPE P1 = -1|TYPE T1 = -2|TYPE P2 = -13|TYPE T2 = -14|")

def TRPT(s, Dn = 150, A = 800.0, TY = -1, ID = "TRPT", **kwargs):
  """
  """
  # Check member profile as standard
  valid_type = [-1, -2, -13, -14]
  if (TY not in valid_type):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type for trunnion profile!")
    print("BI: -1, -2, -13, -14")
    return

  baseplate2 = {
    25:   {"A": 90.00, "t": 6.0},
    40:   {"A": 90.00, "t": 6.0},
    50:   {"A": 140.0, "t": 6.0},
    80:   {"A": 140.0, "t": 6.0},
    100:  {"A": 160.0, "t": 9.0},
    150:  {"A": 220.0, "t": 12.0},
    200:  {"A": 270.0, "t": 12.0},
    250:  {"A": 320.0, "t": 12.0},
    300:  {"A": 370.0, "t": 12.0},
    350:  {"A": 400.0, "t": 16.0},
    400:  {"A": 450.0, "t": 16.0},
    450:  {"A": 500.0, "t": 22.0},
    500:  {"A": 550.0, "t": 22.0},
    600:  {"A": 650.0, "t": 25.0},
    700:  {"A": 760.0, "t": 28.0},
  }

  Dz = pipe_size[Dn]
  Qn = trunnion_size[Dn]
  Qz = pipe_size[Qn]

  plA = baseplate[Qn]["A"]
  plB = baseplate[Qn]["B"]
  plt = baseplate[Qn]["t"]
  plh = baseplate[Qn]["h"]
  pl1 = BOX(s, L = plA, W = plt, H = plA)
  positions = [
    (plB/2,  plB/2,  -plt/2),
    (-plB/2, plB/2,  -plt/2),
    (-plB/2, -plB/2, -plt/2),
    (plB/2,  -plB/2, -plt/2),
  ]
  for pos in positions:
    cy1 = CYLINDER(s, R = plh/2, H = plt, O = 0.0)
    cy1.translate(pos)
    pl1.subtractFrom(cy1)
    cy1.erase()

  F1 = A + Dz * 1.5

  pl2A = baseplate2[Qn]["A"]
  pl2t = baseplate2[Qn]["t"]
  pl2 = BOX(s, L = pl2A, W = pl2t, H = pl2A)

  if (TY == -1):
    pl2.erase()
    m1 = CYLINDER(s, R = Qz/2, H = F1, O = 0.0)
    pl1.translate((0.0, 0.0, plt/2))
    m1.uniteWith(pl1)
    pl1.erase()

    m1.translate((0.0, 0.0, -F1))
    sP2 = (0.0, 0.0, -F1)
    sP3 = (plA/2, 0.0, -F1)
    sLD = [(0.0, Qz/2, A - F1), (0.0, Qz/2, -F1)]

  elif (TY == -2):
    pl2.erase()
    m1 = CYLINDER(s, R = Qz/2, H = A, O = 0.0)
    pl1.translate((0.0, 0.0, plt/2))
    m1.uniteWith(pl1)
    pl1.erase()

    m1.translate((0.0, 0.0, -A))
    sP2 = (0.0, 0.0, -A)
    sP3 = (plA/2, 0.0, -A)
    sLD = [(0.0, Qz/2, 0.0), (0.0, Qz/2, -A)]

  elif (TY == -13):
    pl1.erase()
    m1 = CYLINDER(s, R = Qz/2, H = F1, O = 0.0)
    pl2.translate((0.0, 0.0, pl2t/2))
    m1.uniteWith(pl2)
    pl2.erase()

    m1.translate((0.0, 0.0, -F1))
    sP2 = (0.0, 0.0, -F1)
    sP3 = (pl2A/2, 0.0, -F1)
    sLD = [(0.0, Qz/2, A - F1), (0.0, Qz/2, -F1)]

  elif (TY == -14):
    pl1.erase()
    m1 = CYLINDER(s, R = Qz/2, H = A, O = 0.0)
    pl2.translate((0.0, 0.0, pl2t/2))
    m1.uniteWith(pl2)
    pl2.erase()

    m1.translate((0.0, 0.0, -A))
    sP2 = (0.0, 0.0, -A)
    sP3 = (pl2A/2, 0.0, -A)
    sLD = [(0.0, Qz/2, 0.0), (0.0, Qz/2, -A)]

  s.setPoint((0.0, 0.0, 0.0), (1.0, 0.0, 0.0))
  # For Q1
  s.setPoint(sP2, (0.0, 1.0, 0.0))
  # For P1
  s.setPoint(sP3, (0.0, 1.0, 0.0))
  s.setLinearDimension('A', sLD[0], sLD[1])


def ConvertProfile(BI):
  """
  Convert profile
  """

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  return BF

def PIPE_PAD(header, branch):
  """
  """
  table = {
    50: {40: {"C": 160.0, "L": 110.0},
         50: {"C": 155.0, "L": 120.0}},
    80: {50: {"C": 126.0, "L": 120.0}, 
         80: {"C": 221.0, "L": 170.0}},
    100: {50: {"C": 124.0, "L": 120.0}, 
          80: {"C": 183.0, "L": 170.0},
          100: {"C": 286.0, "L": 220.0}},
    150: {50: {"C": 122.0, "L": 120.0}, 
          80: {"C": 175.0, "L": 170.0},
          100: {"C": 232.0, "L": 220.0},
          150: {"C": 426.0, "L": 330.0}},
    200: {50: {"C": 121.0, "L": 120.0}, 
          80: {"C": 173.0, "L": 170.0},
          100: {"C": 226.0, "L": 220.0},
          150: {"C": 354.0, "L": 330.0},
          200: {"C": 555.0, "L": 430.0}},
    250: {50: {"C": 121.0, "L": 120.0}, 
          80: {"C": 172.0, "L": 170.0},
          100: {"C": 224.0, "L": 220.0},
          150: {"C": 343.0, "L": 330.0},
          200: {"C": 465.0, "L": 430.0},
          250: {"C": 696.0, "L": 540.0}},
    300: {50: {"C": 121.0, "L": 120.0}, 
          80: {"C": 171.0, "L": 170.0},
          100: {"C": 223.0, "L": 220.0},
          150: {"C": 339.0, "L": 330.0},
          200: {"C": 452.0, "L": 430.0},
          250: {"C": 592.0, "L": 540.0},
          300: {"C": 825.0, "L": 640.0}},
    350: {50: {"C": 121.0, "L": 120.0}, 
          80: {"C": 171.0, "L": 170.0},
          100: {"C": 222.0, "L": 220.0},
          150: {"C": 337.0, "L": 330.0},
          200: {"C": 447.0, "L": 430.0},
          250: {"C": 578.0, "L": 540.0},
          300: {"C": 723.0, "L": 640.0},
          350: {"C": 913.0, "L": 710.0}},
    400: {50: {"C": 121.0, "L": 120.0}, 
          80: {"C": 171.0, "L": 170.0},
          100: {"C": 222.0, "L": 220.0},
          150: {"C": 336.0, "L": 330.0},
          200: {"C": 442.0, "L": 430.0},
          250: {"C": 566.0, "L": 540.0},
          300: {"C": 691.0, "L": 640.0},
          350: {"C": 787.0, "L": 710.0},
          400: {"C": 1042.0, "L": 810.0}},
    450: {50: {"C": 120.0, "L": 120.0}, 
          80: {"C": 170.0, "L": 170.0},
          100: {"C": 222.0, "L": 220.0},
          150: {"C": 334.0, "L": 330.0},
          200: {"C": 439.0, "L": 430.0},
          250: {"C": 560.0, "L": 540.0},
          300: {"C": 676.0, "L": 640.0},
          350: {"C": 761.0, "L": 710.0},
          400: {"C": 905.0, "L": 810.0},
          450: {"C": 1171.0, "L": 910.0}},
    500: {50: {"C": 120.0, "L": 120.0}, 
          80: {"C": 170.0, "L": 170.0},
          100: {"C": 221.0, "L": 220.0},
          150: {"C": 334.0, "L": 330.0},
          200: {"C": 438.0, "L": 430.0},
          250: {"C": 555.0, "L": 540.0},
          300: {"C": 667.0, "L": 640.0},
          350: {"C": 748.0, "L": 710.0},
          400: {"C": 875.0, "L": 810.0},
          450: {"C": 1022.0, "L": 910.0},
          500: {"C": 1300.0, "L": 1010.0}},
    550: {50: {"C": 120.0, "L": 120.0}, 
          80: {"C": 170.0, "L": 170.0},
          100: {"C": 221.0, "L": 220.0},
          150: {"C": 333.0, "L": 330.0},
          200: {"C": 436.0, "L": 430.0},
          250: {"C": 552.0, "L": 540.0},
          300: {"C": 661.0, "L": 640.0},
          350: {"C": 739.0, "L": 710.0},
          400: {"C": 859.0, "L": 810.0},
          450: {"C": 988.0, "L": 910.0},
          500: {"C": 1140.0, "L": 1010.0},
          550: {"C": 1429.0, "L": 1110.0}},
    600: {50: {"C": 120.0, "L": 120.0}, 
          80: {"C": 170.0, "L": 170.0},
          100: {"C": 221.0, "L": 220.0},
          150: {"C": 333.0, "L": 330.0},
          200: {"C": 435.0, "L": 430.0},
          250: {"C": 550.0, "L": 540.0},
          300: {"C": 657.0, "L": 640.0},
          350: {"C": 734.0, "L": 710.0},
          400: {"C": 849.0, "L": 810.0},
          450: {"C": 970.0, "L": 910.0},
          500: {"C": 1103.0, "L": 1010.0},
          550: {"C": 1258.0, "L": 1110.0},
          600: {"C": 1558.0, "L": 1210.0}},
  }

  return table[header][branch]