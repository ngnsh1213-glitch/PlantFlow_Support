## GD2
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
@param(Dn=LENGTH, TooltipLong="Nominal Pipe Size")
@param(F1=LENGTH, TooltipLong="F1 length")
@param(B=LENGTH, TooltipLong="B length")
@param(D=LENGTH, TooltipLong="D length")
@param(P1=LENGTH, TooltipLong="P1 length")
@param(TY=LENGTH, TooltipLong="|TYPE B = -2|")

def GD2(s, Dn = 100, F1 = 800.0, B = 300.0, D = 300.0, P1 = 0.0,
  TY = -2, ID = "GD2", **kwargs):
  """
  """
  if (TY != -2):
    e1 = ERROR_SHAPE(s, R = 100.0)
    print("Please input valid type!")
    print("TY: -2 (TYPE B Only)")
    return


  F2 = B + D
  A2 = (F1 - 100.0) / 2
  Dz = pipe_size[Dn]

  # TYPE B (-2) Only
  BI1 = 215
  BI2 = 16

  gap = 10.0

  BFc = ConvertProfile(BI1)
  BFl = ConvertProfile(BI2)
  m11 = C_SHAPE(s, BF = BFc, LE = F1 - gap)
  m12 = C_SHAPE(s, BF = BFc, LE = F1 - gap)
  m21 = L_SHAPE(s, BF = BFl, LE = F2)
  m22 = L_SHAPE(s, BF = BFl, LE = F2 - 2*gap)

  cFa1 = profile[BI1]["Fa1"]
  cFa2 = profile[BI1]["Fa2"]

  m11.rotateZ(-90).rotateY(90).translate((0.0, 0.0, -cFa1/2))
  m12.rotateZ(90).rotateY(90).translate((0.0, F2 - 2*cFa2, -cFa1/2))
  m21.rotateZ(90).rotateX(90).translate((F1 - A2, F2 - cFa2, 0.0))
  m22.rotateZ(90).rotateX(90).translate((F1, F2 - cFa2 - gap, 0.0))

  m11.uniteWith(m12)
  m11.uniteWith(m21)
  m11.uniteWith(m22)
  m12.erase()
  m21.erase()
  m22.erase()

  m11.translate((-F1 - Dz/2 - P1, cFa2 - D, 0.0))

  # Set point
  s.setPoint((0.0, 0.0, 0.0), (0.0, 0.0, 1.0))

  # For F2 and datum
  s.setPoint((-F1 - Dz/2 - P1, cFa2 - D, -cFa1 / 2), (0.0, 1.0, 0.0))
  # For F1
  s.setPoint((-F1 - Dz/2 - P1, B - cFa2, 0.0), (0.0, 1.0, 0.0))
  # For F3
  s.setPoint((-A2 - Dz/2 - P1, 0.0, 0.0), (0.0, 1.0, 0.0))
  # For F4
  s.setPoint((-Dz/2 - P1, B - cFa2 - 50.0, 0.0), (0.0, 1.0, 0.0))


  s.setLinearDimension('F1', (-Dz/2 - P1, B, 0.0), (-F1 - Dz/2 - P1, B, 0.0))
  s.setLinearDimension('B', (-Dz/2 - P1, 0.0, 0.0), (-Dz/2 - P1, B, 0.0))
  s.setLinearDimension('D', (-Dz/2 - P1, 0.0, 0.0), (-Dz/2 - P1, -D, 0.0))

# =====================================================================

def ConvertProfile(BI):
  """
  Convert profile
  """

  BPs = str(BI)
  BPn = int(BPs[0])
  BF  = int(BPs[1:])
  return BF