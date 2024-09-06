from fastapi import FastAPI, WebSocket
from fastapi.responses import StreamingResponse
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io

app = FastAPI()

# Global variable to store the latest frame received from WebSocket
current_frame = None

@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    global current_frame
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

                # Store the latest frame in the global variable
                current_frame = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)

                # Display the image using OpenCV (optional for local viewing)
                cv2.imshow("Live Stream", current_frame)

                # Use a small delay to allow the OpenCV window to refresh
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break  # Optional: break loop on 'q' key press to close the window

    except KeyError as e:
        print(f"Error receiving message: {e}")
    except Exception as e:
        print(f"Unexpected error: {e}")
    finally:
        cv2.destroyAllWindows()


@app.get("/video_feed")
async def video_feed():
    """
    HTTP endpoint to stream the video frames as MJPEG.
    """
    global current_frame

    async def frame_generator():
        while True:
            if current_frame is not None:
                # Encode the current frame as JPEG
                ret, jpeg = cv2.imencode('.jpg', current_frame)
                if not ret:
                    continue

                # Convert to bytes
                frame_bytes = jpeg.tobytes()

                # Yield MJPEG stream format with boundary
                yield (b'--frame\r\n'
                       b'Content-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n\r\n')

    return StreamingResponse(frame_generator(), media_type="multipart/x-mixed-replace; boundary=frame")


if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
