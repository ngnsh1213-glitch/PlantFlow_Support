## TRS
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

@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member M profile")
@param(A=LENGTH, TooltipLong="A length")
@param(T=LENGTH, TooltipLong="Insulation thickness")

def TRS(s, Dn = 150, BI = 215, A = 800.0, T = 0.0, ID = "TRS", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [215]
  if BI not in valid_member:
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 215")
    return

  # standard pipe Dz list
  pipe_size = {
    15: 21.30,
    20: 26.70,
    25: 33.40,
    40: 48.30,
    50: 60.30,
    65: 73.00,
    80: 88.90,
    100: 114.3,
    125: 150.3,
    150: 168.3,
    200: 219.1,
    250: 273.1,
    300: 323.9,
    350: 355.6,
    400: 406.4,
    450: 457.2,
    500: 508.0,
    550: 558.8,
    600: 609.6,
    650: 660.0,
    700: 711.0,
    800: 813.0,
    850: 864.0,
  }

  pipe_wall = {
    15: 5.0,
    20: 5.0,
    25: 5.0,
    40: 5.0,
    50: 5.0,
    65: 5.0,
    80: 5.0,
    100: 7.0,
    125: 7.0,
    150: 7.0,
    200: 7.0,
    250: 7.0,
    300: 7.0,
    350: 9.0,
    400: 9.0,
    450: 9.0,
    500: 9.0,
    550: 9.0,
    600: 9.0,
    650: 10.0,
    700: 10.0,
    800: 10.0,
    850: 10.0,
  }

  trunnion_size = {
    50: 40,
    80: 50,
    100: 50,
    125: 80,
    150: 100,
    200: 100,
    250: 150,
    300: 200,
  }

  support_list = {
    50: {"B": 65.0, "D": 175.0},
    80: {"B": 80.0, "D": 190.0},
    100: {"B": 90.0, "D": 200.0},
    125: {"B": 100.0, "D": 210.0},
    150: {"B": 110.0, "D": 220.0},
    200: {"B": 135.0, "D": 245.0},
    250: {"B": 165.0, "D": 275.0},
    300: {"B": 190.0, "D": 300.0},
  }

  # Profile
  profile = {
    16:  {"F": 65.0,  "T": 6.0},
    17:  {"F": 75.0,  "T": 9.0},
    210: {"Fa1": 100.0, "Fa2": 50.0, "T1": 5.0, "T2": 7.5},
    212: {"Fa1": 125.0, "Fa2": 65.0, "T1": 6.0, "T2": 8.0},
    215: {"Fa1": 150.0, "Fa2": 75.0, "T1": 6.5, "T2": 10.0},
    315: {"F": 150.0, "W": 7.0, "T": 10.0},
    320: {"F": 200.0, "W": 8.0, "T": 12.0},
  }

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])

  BIl = 16
  B = support_list[Dn]["B"] + T
  F1l = A + B + 75.0
  if (BPn == 2):
    m11 = C_SHAPE(s, BF = BF, LE = F1l)
    m12 = C_SHAPE(s, BF = BF, LE = F1l)
    Fa1 = profile[BI]["Fa1"]
    Fa2 = profile[BI]["Fa2"]

    m11.rotateZ(-90).rotateY(90)
    m12.rotateZ(90).rotateY(90)
    m12.translate((0.0, 2*B, 0.0))

  F2l = 2 * B + 150.0
  BFl = ConvertProfile(BIl)
  m21 = L_SHAPE(s, BF = BFl, LE = F2l)
  m21.rotateX(90).translate((F1l - 75.0, F2l - 75.0, Fa1/2))

  D = support_list[Dn]["D"] + T
  DnQ = trunnion_size[Dn]
  Qs = pipe_size[DnQ]
  m31 = CYLINDER(s, R = Qs/2, H = 2 * D, O = 0.0)
  m31.rotateX(-90).translate((A, B - D, Qs/2 + Fa1/2))

  pad_size = PIPE_PAD(Dn, DnQ)
  pC = pad_size["C"]
  pL = pad_size["L"]
  pR = pipe_size[Dn]/2 + pipe_wall[Dn]
  m41 = CYLINDER(s, R = pR, H = pC, O = 0.0)
  m41.translate((A, B, Fa1/2 + Qs/2 - pC/2))
  m42 = CYLINDER(s, R = pR, H = pC, O = 0.0)
  m42.translate((A, B, Fa1/2 + Qs/2 - pC/2))

  o41 = CYLINDER(s, R1 = 4 * pC/2, R2 = 4 * pL/2, H = 2*D, O = 0.0)
  o42 = CYLINDER(s, R1 = pC/2, R2 = pR/1.5, H = 2*D, O = 0.0)
  o41.subtractFrom(o42)
  o42.erase()
  o41.rotateY(90).translate((A - B, B - pR, Qs/2 + Fa1/2))
  m41.subtractFrom(o41)
  o41.erase()

  o43 = CYLINDER(s, R1 = 4 * pC/2, R2 = 4 * pL/2, H = 2*D, O = 0.0)
  o44 = CYLINDER(s, R1 = pC/2, R2 = pR/1.5, H = 2*D, O = 0.0)
  o43.subtractFrom(o44)
  o44.erase()
  o43.rotateY(90).translate((A - B, B + pR, Qs/2 + Fa1/2))
  m42.subtractFrom(o43)
  o43.erase()

  m11.uniteWith(m12)
  m11.uniteWith(m21)
  m11.uniteWith(m31)
  m11.uniteWith(m41)
  m11.uniteWith(m42)
  m12.erase()
  m21.erase()
  m31.erase()
  m41.erase()
  m42.erase()

  o45 = CYLINDER(s, R = pipe_size[Dn]/2, H = 2*D, O = 0.0)
  o45.translate((A, B, 0.0))
  m11.subtractFrom(o45)
  o45.erase()

  m11.translate((-A, -B, -Qs/2 - Fa1/2))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

  # For F1 and datum
  s.setPoint((-A, -B, -Qs/2 - Fa1/2), (0.0, 0.0, 1.0))
  # For F2
  s.setPoint((-A, B, -Qs/2 - Fa1/2), (0.0, 1.0, 0.0))
  # For F3
  s.setPoint((B, -B, -Qs/2), (0.0, 1.0, 0.0))
  # For Q1
  s.setPoint((0.0, B, 0.0), (0.0, 1.0, 0.0))
  # For Q2
  s.setPoint((0.0, -B, 0.0), (0.0, 1.0, 0.0))
  # For saddle P1
  s.setPoint((0.0, pR, 0.0), (0.0, 1.0, 0.0))
  # For saddle P2
  s.setPoint((0.0, -pR, 0.0), (0.0, 1.0, 0.0))
  s.setLinearDimension('A', (0.0, B, -Qs/2), (-A, B, -Qs/2))


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