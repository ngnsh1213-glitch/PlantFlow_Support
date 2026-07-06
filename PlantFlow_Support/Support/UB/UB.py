## UB
## Developed by Noh Seunghwan
## Last Modified: Apr 29, 2026
## V:\4_부서관리\9_배관팀\03 설계자료\03_02 Support\PIPE SUPPORT STANDARD_R0_260311
## Linked Backend: [[scan_HANTEC_cs_20260426_144159|HANTEC]], [[scan_BOMs_cs_20260426_144100|BOMs]], [[scan_PlantAutoCoding_cs_20260426_144237|PlantAutoCoding]]
##--------------------------------
from aqa.math import *
from varmain.primitiv import * 
from varmain.var_basic import * 
from varmain.custom import *
from varmain.divsub.cnut6_001 import *

import os
import sys
directory = os.path.dirname(os.path.abspath(__file__)) + "/../library"
sys.path.insert(0, directory)
from support_data import pipe_size, fastener_size

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="1")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal sPipe Size")
@param(C=LENGTH, TooltipLong="")
@param(L=LENGTH, TooltipLong="")
@param(S=LENGTH, TooltipLong="")
@param(R=LENGTH, TooltipLong="")
@param(B=LENGTH, TooltipLong="L for type type B")
@param(SN=LENGTH, TooltipLong="SN length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|TYPE C = -3|NONE = 0")

def UB(s, Dn = 50, C = 76.0, L = 70.0, S = 65.0, R = 31.7, 
  B = 120.0, SN = 0.0, TY = 0, ID = "UB", **kwargs):
  """
  """
  if (TY not in [0, -1, -2, -3]):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: 0, -1, -2, -3")
    return

  valid_type = [0, -1, -2, -3]
  if (TY not in valid_type):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid support type!")
    print("TY: 0, -1, -2, -3")
    return

  d = fastener_size[Dn]["B"]
  o1 = TORUS(s, R1 = R + d/2, R2 = d/2)
  sb1 = BOX(s, L = 2*R + 2*d, W = d, H = 2*R + 2*d)
  sb1.translate((-R - d, 0.0, 0.0))
  o1.subtractFrom(sb1)
  sb1.erase()

  c1 = CYLINDER(s, R = d/2, H = L, O = 0.0)
  c1.rotateY(-90).translate((0.0, R + d/2, 0.0))
  c2 = CYLINDER(s, R = d/2, H = L, O = 0.0)
  c2.rotateY(-90).translate((0.0, -R - d/2, 0.0))
  o1.uniteWith(c1)
  c1.erase()
  o1.uniteWith(c2)
  c2.erase()

  psize = pipe_size[Dn]
  dt1 = R - psize/2
  dt2 = psize/2 - dt1

  b1 = BOX(s, L = 60.0 + C, W = 50.0, H = 3.0)
  b1.translate((-dt2 - 1.5, 0.0, 0.0))
  o1.uniteWith(b1)
  b1.erase()

  nS = fastener_size[Dn]["N"]["S"]
  nH = fastener_size[Dn]["N"]["H"]
  n1 = HEXAGON(s, R = nS/2, H = nH)
  n2 = HEXAGON(s, R = nS/2, H = nH)

  wD = fastener_size[Dn]["W"]["D"]
  wH = fastener_size[Dn]["W"]["H"]
  w1 = CYLINDER(s, R = wD/2, H = wH, O = 0.0)
  w1.translate((0.0, 0.0, nH))
  n1.uniteWith(w1)
  w1.erase()
  n1.rotateY(90).translate((-L + 10.0, R + d/2, 0.0))

  w2 = CYLINDER(s, R = wD/2, H = wH, O = 0.0)
  w2.translate((0.0, 0.0, nH))
  n2.uniteWith(w2)
  w2.erase()
  n2.rotateY(90).translate((-L + 10.0, -R - d/2, 0.0))

  o1.uniteWith(n1)
  n1.erase()

  o1.uniteWith(n2)
  n2.erase()

  if (TY in [-1, -2, -3]):
    if (Dn > 100):
      e1 = ERROR_SHAPE(s, R = 100.0)
      print("Error: Dn > 100 is not supported for this type.")
      return

  if (TY == -1):
    fr1 = L_SHAPE(s, BF = 5, LE = B)
    fr1.rotateX(-90).rotateY(180).translate((-dt2 - 3, -B/2, -30.0))
    o1.uniteWith(fr1)
    fr1.erase()

  elif (TY == -2):
    frT = 6.0
    frA = 50.0
    if (Dn == 100):
      frT = 9.0
      frA = 65.0

    fr1 = BOX(s, L = B, W = frA, H = frT)
    fr1.translate((-dt2 - 3 - frT/2, 0.0, 0.0))
    o1.uniteWith(fr1)
    fr1.erase()

  elif (TY == -3):
    pass

  elif (TY == 0):
    pass

  o1.translate((-dt1, 0.0, 0.0)).rotateY(-90)
  o1.translate((SN, 0.0, 0.0))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (1.0, 0.0, 0.0))


