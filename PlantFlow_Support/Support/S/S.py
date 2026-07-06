## S
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

from support_data import pipe_size, profile, shoe_size_data

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
@param(T1=LENGTH, TooltipLong="T1 length")
@param(SN=LENGTH, TooltipLong="SN length")

def S(s, Dn = 150, H = 100.0, B = 150.0, B1 = 50.0, 
  L1 = 300.0, T1 = 9.0, SN = 0.0, ID = "S", **kwargs):
  """
  """
  # Data is now imported from support_data.py

  # shoe_size = SHOE_SIZE(Dn, H)
  # B = shoe_size["B"]
  # T1 = shoe_size["T1"]
  Dz = pipe_size[Dn]

  if (Dn < 250):
    # T-Shape Support
    m1 = TRANS_BOX(s, L = B, W = T1, H = L1, BASE = "BOTTOM_05")
    m2 = TRANS_BOX(s, L = T1, W = H, H = L1, BASE = "BOTTOM_05")
    m1.uniteWith(m2); m2.erase()
    m1.translate((SN, 0.0, -Dz/2 - H))

  else:
    # Reinforced Shoe Support (Dn >= 250)
    # 1. Create Base and Sub-components
    m1 = TRANS_BOX(s, L = B + 60.0, W = T1, H = L1, BASE = "BOTTOM_05") # Base Plate
    
    # 2. Assemble Components (Top plate and Side webs)
    comp_configs = [
      (B, T1, B1, 0.0),                     # Top Plate (L, W, Z_offset, Y_offset)
      (T1, H + Dz/2, T1, B/2 - T1/2),       # Side Web 1
      (T1, H + Dz/2, T1, T1/2 - B/2)        # Side Web 2
    ]
    
    for l_val, w_val, z_off, y_off in comp_configs:
      temp_m = TRANS_BOX(s, L = l_val, W = w_val, H = L1, BASE = "BOTTOM_05")
      temp_m.translate((0.0, y_off, z_off))
      m1.uniteWith(temp_m); temp_m.erase()

    # 3. Pipe Saddle Subtraction
    c1 = CYLINDER(s, R = Dz/2, H = L1, O = 0.0)
    c1.rotateY(90).translate((-L1/2, 0.0, Dz/2 + H + T1))
    m1.subtractFrom(c1); c1.erase()
    
    # 4. Final Positioning
    m1.translate((SN, 0.0, -Dz/2 - H - T1))
  
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