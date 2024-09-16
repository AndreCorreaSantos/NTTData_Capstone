# models.py

from pydantic import BaseModel

class ColorData(BaseModel):
    r: int
    g: int
    b: int

class GuiColorsData(BaseModel):
    background_color: ColorData
    text_color: ColorData

class PositionData(BaseModel):
    x: float
    y: float
    z: float

class RotationData(BaseModel):
    x: float
    y: float
    z: float
    w: float

class FrameDataMessage(BaseModel):
    type: str
    gui_colors: GuiColorsData
    object_position: PositionData = None

class ImageDataMessage(BaseModel):
    type: str
    position: PositionData
    rotation: RotationData
    imageData: str
    fx: float
    fy: float
    cx: float
    cy: float
