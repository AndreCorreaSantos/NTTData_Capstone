from fastapi import FastAPI, WebSocket
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io

app = FastAPI()

@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()

    try:
        while True:
            # Receive a message
            message = await websocket.receive()
            data = message.get("bytes")

            if data:
                # Decode the PNG data to an image using Pillow (PIL)
                image = Image.open(io.BytesIO(data))
                image_np = np.array(image)  # Convert PIL image to NumPy array

                # Display the image using OpenCV
                cv2.imshow("Live Stream", cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR))
                
                # Use a small delay to allow the OpenCV window to refresh
                # This ensures it doesn't depend on user input and updates continuously
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break  # Optional: break loop on 'q' key press to close the window

    except KeyError as e:
        print(f"Error receiving message: {e}")
    except Exception as e:
        print(f"Unexpected error: {e}")
    finally:
        cv2.destroyAllWindows()

if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
