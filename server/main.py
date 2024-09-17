from os import write
from datetime import datetime
from fastapi import FastAPI, WebSocket
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io
from ultralytics import YOLO
import json
import base64
import danger_analysis
import aiofiles
import asyncio

from image_processing import process_image, calculate_background_colors

app = FastAPI()
model = YOLO("yolov8n.pt")
now = datetime.now()

async def write_to_file_async(path, image_data):
    image_as_jpeg_buffer = io.BytesIO()
    image_data.save(image_as_jpeg_buffer, format="JPEG")
    async with aiofiles.open(path, "wb") as file:
        await file.write(image_as_jpeg_buffer.getbuffer())

@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()

    try:
        loop = asyncio.get_running_loop()
        asyncio.create_task(danger_analysis.run_analyzer())
        while True:
            json_message = await websocket.receive_text()
            data = json.loads(json_message)

            image_type = data.get('type')
            image_data_base64 = data.get('imageData')
            position = data.get('position')
            rotation = data.get('rotation')

            image_data_bytes = base64.b64decode(image_data_base64)
            image = Image.open(io.BytesIO(image_data_bytes))
            image_np = np.array(image)

            asyncio.create_task(write_to_file_async(f"./gpt/captured_image_{now.strftime("%m/%d/%Y, %H:%M:%S")}", image))

            if image_type == "color":
                current_frame = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)

                # Calculate GUI colors
                gui_back_color, gui_text_color = calculate_background_colors(current_frame)

                # Initialize object_position as None
                object_position_data = None

                # Object detection
                results = model(current_frame, verbose=False)
                for detection in results:
                    if detection is not None:
                        detection_json = detection.tojson()
                        result_json = json.loads(detection_json)
                        if result_json:
                            object_position = process_image(current_frame, result_json, rotation, position)
                            object_position_data = {
                                "x": object_position['x'],
                                "y": object_position['y'],
                                "z": object_position['z']
                            }
                            break  # Process only the first detected object

                # Prepare the combined JSON message
                frame_data_message = {
                    "type": "frame_data",
                    "gui_colors": {
                        "background_color": {
                            "r": gui_back_color[0],
                            "g": gui_back_color[1],
                            "b": gui_back_color[2]
                        },
                        "text_color": {
                            "r": gui_text_color[0],
                            "g": gui_text_color[1],
                            "b": gui_text_color[2]
                        }
                    },
                    "object_position": object_position_data  # None if no object detected
                }

                await websocket.send_text(json.dumps(frame_data_message))

                cv2.imshow("Color Image", current_frame)
                cv2.waitKey(1)

            elif image_type == "depth":
                current_depth_frame = image_np
                cv2.imshow("Depth Image", current_depth_frame)
                cv2.waitKey(1)

    except Exception as e:
        print(f"Error: {e}")
    finally:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
