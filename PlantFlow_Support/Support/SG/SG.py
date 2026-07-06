## SG
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

from support_data import pipe_size, guide_profile, profile, shoe_size_data

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="1")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(H=LENGTH, TooltipLong="H length")
@param(B=LENGTH, TooltipLong="B length")
@param(B1=LENGTH, TooltipLong="B1 length")
@param(L1=LENGTH, TooltipLong="L1 length")
@param(L2=LENGTH, TooltipLong="L2 length")
@param(T1=LENGTH, TooltipLong="T1 length")
@param(CL=LENGTH, TooltipLong="CL length")
@param(SN=LENGTH, TooltipLong="SN length")

def SG(s, Dn = 150, H = 100.0, B = 150.0, B1 = 50.0, 
  L1 = 300.0, L2 = 60.0, T1 = 9.0, CL = 3.0, SN = 0.0, ID = "SG", **kwargs):
  """
  """
  # Data is now imported from support_data.py

  # Convert profile
  BI  = guide_profile[Dn]
  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  Fa1 = profile[BI]["F"]
  ft  = profile[BI]["T"]
  di1 = (Fa1 / (sqrt(2)/2)) / 2

  # shoe_size = SHOE_SIZE(Dn, H)
  # B = shoe_size["B"]  
  # T1 = shoe_size["T1"]
  Dz = pipe_size[Dn]

  if (Dn < 250):
    m1 = TRANS_BOX(s, L = B, W = T1, H = L1, BASE = "BOTTOM_05")
    m2 = TRANS_BOX(s, L = T1, W = H, H = L1, BASE = "BOTTOM_05")
    m3 = L_SHAPE(s, BF = BF, LE = L2)
    m4 = L_SHAPE(s, BF = BF, LE = L2)

    m3.rotateY(90).translate((-L2/2, -B/2 - CL - ft, Fa1))
    m4.rotateZ(-90).rotateY(90).translate((-L2/2, B/2 + CL + ft, Fa1))

    m1.uniteWith(m2)
    m1.uniteWith(m3)
    m1.uniteWith(m4)
    m2.erase()
    m3.erase()
    m4.erase()

  elif (Dn >= 250):
    DBx = B + 60.0
    m1 = TRANS_BOX(s, L = B, W = T1, H = L1, BASE = "BOTTOM_05")
    m11 = TRANS_BOX(s, L = DBx, W = T1, H = L1, BASE = "BOTTOM_05")
    m2 = TRANS_BOX(s, L = T1, W = H + Dz/2, H = L1, BASE = "BOTTOM_05")
    m3 = TRANS_BOX(s, L = T1, W = H + Dz/2, H = L1, BASE = "BOTTOM_05")
    m4 = L_SHAPE(s, BF = BF, LE = L2)
    m5 = L_SHAPE(s, BF = BF, LE = L2)

    m1.translate((0.0, 0.0, B1))
    m2.translate((0.0, B/2 - T1/2, T1))
    m3.translate((0.0, T1/2 - B/2, T1))
    m4.rotateY(90).translate((-L2/2, -DBx/2 - CL - ft, Fa1))
    m5.rotateZ(-90).rotateY(90).translate((-L2/2, DBx/2 + CL + ft, Fa1))

    m1.uniteWith(m2)
    m1.uniteWith(m11)
    m1.uniteWith(m3)
    m1.uniteWith(m4)
    m1.uniteWith(m5)
    m2.erase()
    m3.erase()
    m4.erase()
    m5.erase()

    c1 = CYLINDER(s, R = Dz/2, H = L1, O = 0.0)
    c1.rotateY(90).translate((-L1/2, 0.0, Dz/2 + H))
    m1.subtractFrom(c1)
    c1.erase()

  m1.translate((SN, 0.0, -Dz/2 - H))
  s.setPoint((0.0, 0.0, 0.0), (1.0, 0.0, 0.0))
  s.setLinearDimension("H", (SN, 0.0, -Dz/2), (SN, 0.0, -Dz/2 - H))

def SHOE_SIZE(Dn = 100.0, H = 100.0):
  """
  """
  if (0.0 < H < 125.0):
    H = 100
  elif (125 <=  H < 175):
    H = 150
  elif (175 <= H < 225):
    H = 200
  elif (H >= 225):
    H = 250

  if (20 <= Dn <= 40):
    Dn = 20
  elif (50 <= Dn <= 150):
    Dn = 50
  elif (300 <= Dn <= 400):
    Dn = 300
  elif (450 <= Dn <= 600):
    Dn = 450

  key = str(Dn) + str(H)

  if (key == "200250"):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("No dimension detail for shoe with pipe 200A and H > 250.")
    return

  return shoe_size_data[key]
