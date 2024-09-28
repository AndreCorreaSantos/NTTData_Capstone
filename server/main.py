# server_main.py

from fastapi import FastAPI, WebSocket
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io
from ultralytics import YOLO
import json
import base64
import aiofiles
import os
import locks

import asyncio
from image_processing import process_image, calculate_background_colors

from transformers import pipeline
from PIL import Image

depth_pipe = pipeline(task="depth-estimation", model="depth-anything/Depth-Anything-V2-Small-hf", device=0)


# import danger_analysis
from datetime import datetime

app = FastAPI()
model = YOLO("yolov8n.pt")

now = datetime.now()


PERSON_CLASS_NAME = "person"

async def write_to_file_async(path, image_data):
    async with locks.file_lock:
        temp_path = path + '.tmp'
        image_as_jpeg_buffer = io.BytesIO()
        image_data.save(image_as_jpeg_buffer, format="JPEG")
        async with aiofiles.open(temp_path, "wb") as file:
            await file.write(image_as_jpeg_buffer.getbuffer())
        # Rename the temporary file to the actual file name after writing is complete
        os.rename(temp_path, path)

"""
@app.websocket("/danger/")
async def websocket_danger_detection(websocket: WebSocket):
    await websocket.accept()

    try:
        asyncio.create_task(danger_analysis.run_analyzer(websocket))
    except Exception as e:
        print(f"Error: {e}")
"""



@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()

    try:
        # loop = asyncio.get_running_loop()
        # asyncio.create_task(danger_analysis.run_analyzer(websocket))
        while True:

            json_message = await websocket.receive_text()
            data = json.loads(json_message)

            image_type = data.get('type')
            image_data_base64 = data.get('imageData')
            position = data.get('position')
            # print("Position: ", position)   
            rotation = data.get('rotation')
            fx = data.get('fx')  # Camera intrinsic fx
            fy = data.get('fy')  # Camera intrinsic fy
            cx = data.get('cx')  # Camera principal point x
            cy = data.get('cy')  # Camera principal point y

            # Validate essential fields
            if image_type is None or image_data_base64 is None:
                print("Missing 'type' or 'imageData' in the received message.")
                continue

            # Decode the image data
            try:
                image_data_bytes = base64.b64decode(image_data_base64)
                image = Image.open(io.BytesIO(image_data_bytes))
                # asyncio.create_task(write_to_file_async(f"./gpt/captured_image_{now.strftime('%m-%d-%Y-%H-%M-%S')}.jpg", image))

                image_np = np.array(image)
            except Exception as e:
                print(f"Failed to decode image data: {e}")
                continue

            if image_type == "color":
                # Convert RGB to BGR for OpenCV
                current_frame = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)

                # Calculate GUI colors
                gui_back_color, gui_text_color = calculate_background_colors(current_frame)

                # Initialize list to hold positions of all detected persons
                object_positions = []

                # Object detection using YOLO
                results = model(current_frame, verbose=False)

                # Depth estimation using Depth anything
                depth_frame = np.array(depth_pipe(image)["depth"])
                
                # open another window to display the depth frame


                # Iterate through all detections
                
                for detection in results:
                    if detection is not None:
                        detection_json = detection.to_json()
                        result_json = json.loads(detection_json)
                        
                        # Assuming result_json is a list of detections
                        for det in result_json:
                            # Check if the detected class is "person"
                            if(det["name"] == "person"):
                                object_position = process_image(
                                    current_frame,
                                    depth_frame,
                                    det,  # Single detection
                                    rotation,
                                    position,
                                    fx,
                                    fy,
                                    cx,
                                    cy,
                                )
                                # print("Object Position: ", object_position)
                                if object_position:
                                    object_positions.append({
                                        "x": object_position['x'],
                                        "y": object_position['y'],
                                        "z": object_position['z']
                                    })

                # Prepare the combined JSON message
                '''DELETAR: PORQUE NÃO COLOCAR A INFORMAÇÃO DO GPT AQUI:
                ELE É ASINCRONO EM RELAÇÃO AO RESTO DO PROGRAMA.
                esperar ele pra mandar nesse mesmo request daria um problema, 
                já que teria que esperar a resposta da openai pra mandar o resto.
                vou tentar criar um endpoint novo só pra stream de dados do caso 3, já 
                que não faz sentido tratar dele aqui'''
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
                    "object_positions": object_positions if object_positions else None  # List or None
                }

                # Send the response back to the client
                print("frame_data_message")
                print(frame_data_message)
                try:
                    async with locks.websocket_lock:
                        await websocket.send_text(json.dumps(frame_data_message))
                except Exception as e:
                    print(f"Failed to send frame data message: {e}")

                # Display the color image (optional, useful for debugging)
                cv2.imshow("Color Image", current_frame)
                # depth_frame_normalized = cv2.normalize(depth_frame, None, 0, 255, cv2.NORM_MINMAX)
                # depth_frame_normalized = np.uint8(depth_frame_normalized)

                # Display the depth image in another window
                cv2.imshow("Depth Image", depth_frame)
                cv2.waitKey(1)

    except Exception as e:
        print(f"Error: {e}")
    finally:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
