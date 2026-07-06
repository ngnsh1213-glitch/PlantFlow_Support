## GD1
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

from support_data import pipe_size_JIS as pipe_size, profile, baseplate_GD1 as baseplate

try:
  from SHAPE import *
except ImportError:
  from SHAPE_v25 import *

@activate(Group="Support", LengthUnit="mm", Ports="2")
@group("MainDimensions")
@param(Dn=LENGTH, TooltipLong="Nominal pipe size")
@param(BI=LENGTH, TooltipLong="Member profile")
@param(A=LENGTH, TooltipLong="A length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE A = -1|TYPE B = -2|")

def GD1(s, Dn = 15, BI = 212, A = 300.0, P1 = 0.0, 
  TY = -2, ID = "GD1", **kwargs):
  """
  """
  # Check member profile as standard
  valid_member = [15, 16, 17, 110, 212]
  if (BI not in valid_member):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid member profile!")
    print("BI: 15, 16, 17, 110, 212")
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
  F1 = A

  if (BPn == 1):
    Fa1 = profile[BI]["F"]
    m1 = L_SHAPE(s, BF = BF, LE = F1)
    m1.rotateZ(-90).rotateY(90).translate((100.0 - A, 0.0, -Dz/2 - P1))
    sPL = [(100.0, -Fa1, -Dz/2 - P1), (100.0 - A, -Fa1, -Dz/2 - P1)]
    pS2 = (100.0 - A, 0.0, -Dz/2 - P1)

  elif (BPn == 2):
    Fa1 = profile[BI]["Fa1"]
    m1 = C_SHAPE(s, BF = BF, LE = F1)
    m1.rotateZ(-90).rotateY(90).translate((100.0 - A, 0.0, -Dz/2 - Fa1/2 - P1))
    sPL = [(100.0, 0.0, -Dz/2 - P1), (100.0 - A, 0.0, -Dz/2 - P1)]
    pS2 = (100.0 - A, 0.0, -Dz/2 - Fa1/2 - P1)

  # If user select type B for this support
  if (TY == -2):
    plA = baseplate[BI]["A"]
    plB = baseplate[BI]["B"]
    plt = baseplate[BI]["t"]
    plh = baseplate[BI]["h"]
    pl1 = BOX(s, L = plA, W = plt, H = plA)
    positions = [
      (plB/2,  -plt/2,  plB/2),
      (-plB/2, -plt/2,  plB/2),
      (-plB/2, -plt/2, -plB/2),
      (plB/2,  -plt/2, -plB/2),
    ]
    for pos in positions:
      cy1 = CYLINDER(s, R = plh/2, H = plt, O = 0.0)
      cy1.rotateX(90)
      cy1.translate(pos)
      pl1.subtractFrom(cy1)
      cy1.erase()

    if (BPn == 1):
      dt1 = profile[BI]["F"]
      pl1.rotateY(90).translate((plt/2 + 100.0 - A, -dt1/2, -dt1/2 - Dz/2 - P1))
      m1.uniteWith(pl1)
      pl1.erase()

    elif (BPn == 2):
      dt1 = profile[BI]["Fa2"]
      pl1.rotateY(90).translate((plt/2 + 100.0 - A, -dt1/2, -Dz/2 - Fa1/2 - P1))
      m1.uniteWith(pl1)
      pl1.erase()

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 1.0, 0.0))

  # For F1 and datum
  s.setPoint(pS2, (0.0, 0.0, 1.0))

  # For dim
  s.setPoint((100.0, 0.0, -Dz/2-P1), (0.0, 0.0, 1.0))

  # Set point for base plate
  if (TY == -2):
    s.setPoint((100.0 - A, 0.0, plA/2 - Dz/2 - Fa1/2 - P1), (0.0, 0.0, 1.0))

  s.setLinearDimension('A', sPL[0], sPL[1])

# =====================================================================

