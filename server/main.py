
import traceback
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

from metric_depth.depth_anything_v2.dpt import DepthAnythingV2
import torch

def load_depth_model():
    model_configs = {
        'vits': {'encoder': 'vits', 'features': 64, 'out_channels': [48, 96, 192, 384]},
        'vitb': {'encoder': 'vitb', 'features': 128, 'out_channels': [96, 192, 384, 768]},
        'vitl': {'encoder': 'vitl', 'features': 256, 'out_channels': [256, 512, 1024, 1024]}
    }

    encoder = 'vitb' # or 'vits', 'vitb'
    dataset = 'hypersim' # 'hypersim' for indoor model, 'vkitti' for outdoor model
    max_depth = 20 # 20 for indoor model, 80 for outdoor model

    model = DepthAnythingV2(**{**model_configs[encoder], 'max_depth': max_depth}).cuda()
    model.load_state_dict(torch.load(f'depth_anything_v2_metric_{dataset}_{encoder}.pth', map_location='cpu'))
    model.eval()
    return model




# import danger_analysis
from datetime import datetime

app = FastAPI()
model = YOLO("yolov8n.pt")

depth_model = load_depth_model()

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
            message = json.loads(json_message)

            image_type = message.get('type')
            image_data_base64 = message.get('imageData')
            data_message = message.get('data')
            inv_mat_message = message.get('invMat')
            ui_screen_corners = message.get('UIScreenCorners')
            flip_colors = message.get('flipColors')

            camera_position = np.array([data_message['x'], data_message['y'], data_message['z']])
            print("Camera Position: ", camera_position)
            inv_mat = np.array([
                [inv_mat_message['e00'], inv_mat_message['e01'], inv_mat_message['e02'], inv_mat_message['e03']],
                [inv_mat_message['e10'], inv_mat_message['e11'], inv_mat_message['e12'], inv_mat_message['e13']],
                [inv_mat_message['e20'], inv_mat_message['e21'], inv_mat_message['e22'], inv_mat_message['e23']],
                [inv_mat_message['e30'], inv_mat_message['e31'], inv_mat_message['e32'], inv_mat_message['e33']]
            ])

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
                print(traceback.format_exc())
                continue

            if image_type == "color":
                # Convert RGB to BGR for OpenCV
                current_frame = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)

                # Calculate GUI colors
                gui_back_color, gui_text_color, interior_roi = (
                    calculate_background_colors(
                        current_frame,
                        ui_screen_corners,
                        flip_colors    
                    ))

                # Initialize list to hold positions of all detected persons
                objects_data = []

                # Object detection using YOLO
                results = model.track(current_frame, verbose=False,persist=True)

                # Depth estimation using Depth anything
                depth_frame = depth_model.infer_image(image_np)
                
                for detection in results:
                    if detection is not None:
                        detection_json = detection.to_json()
                        result_json = json.loads(detection_json)
                        # print("Result JSON: ", result_json) 
                        # Assuming result_json is a list of detections      
                        for det in result_json:
                            print("Det: ", det)
                            # Check if the detected class is "person"
                            if(det["name"] == "person"):
                                print("Person detected")
                                obj_data = process_image(
                                    current_frame,
                                    depth_frame,
                                    det,  # Single detection
                                    inv_mat,
                                    camera_position
                                )
                                # print("Object Position: ", obj_data)
                                if obj_data:
                                    obj_id = "-1"
                                    if det.get('track_id') is not None:
                                        obj_id = det['track_id'] 
                                    
                                    objects_data.append({
                                        "x": obj_data['x'],
                                        "y": obj_data['y'],
                                        "z": obj_data['z'],
                                        "id": obj_id,
                                        "width": obj_data['width'], 
                                        "height": obj_data['height']
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
                    "objects": objects_data if objects_data else None  # List or None
                }

                # Send the response back to the client
                print("frame_data_message")
                print(frame_data_message)
                try:
                    async with locks.websocket_lock:
                        await websocket.send_text(json.dumps(frame_data_message))
                except Exception as e:
                    print(traceback.format_exc())

                # Display the color image (optional, useful for debugging)

                width, height = interior_roi.shape[:2]
                if(width > 0 and height > 0):
                    cv2.imshow("Color Image", current_frame)
                # depth_frame_normalized = cv2.normalize(depth_frame, None, 0, 255, cv2.NORM_MINMAX)
                # depth_frame_normalized = np.uint8(depth_frame_normalized)

                # Normalize depth frame for visualization
                depth_frame_normalized = cv2.normalize(depth_frame, None, 0, 255, cv2.NORM_MINMAX)
                depth_frame_normalized = np.uint8(depth_frame_normalized)

                # Display the normalized depth image
                cv2.imshow("Depth Image", depth_frame_normalized)
                cv2.waitKey(1)

    except Exception as e:
        print(traceback.format_exc())
    finally:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
