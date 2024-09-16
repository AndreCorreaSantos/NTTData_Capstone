from fastapi import FastAPI, WebSocket
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io
from ultralytics import YOLO
import json
import base64

from main import process_image

app = FastAPI()
model = YOLO("yolov8n.pt")



@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()

    try:
        while True:
            # Receive a JSON message
            json_message = await websocket.receive_text()
            data = json.loads(json_message)

            image_type = data.get('type')
            image_data_base64 = data.get('imageData')
            position = data.get('position')
            rotation = data.get('rotation')

            # Decode the image data from Base64
            image_data_bytes = base64.b64decode(image_data_base64)
            image = Image.open(io.BytesIO(image_data_bytes))
            image_np = np.array(image)

            if image_type == "color":
                current_frame = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)
                object_position = process_image(current_frame, model, rotation, position)
                # Send the object position back to the client
                await websocket.send_text(
                    f"object_position {object_position['x']} {object_position['y']} {object_position['z']}"
                )

                cv2.imshow("Color Image", current_frame)
                cv2.waitKey(1)

            elif image_type == "depth":
                current_depth_frame = image_np  # Assuming depth data is single-channel
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
