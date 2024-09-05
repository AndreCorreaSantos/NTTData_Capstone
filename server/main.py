from fastapi import FastAPI, WebSocket
import uvicorn
from PIL import Image
import numpy as np
import cv2
import io
import subprocess
import time

app = FastAPI()




@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()

    ffmpeg = None
    try:
        while True:
            # Receive a message
            message = await websocket.receive()
            data = message.get("bytes")

            if data:
                # Decode the PNG data to an image using Pillow (PIL)
                image = Image.open(io.BytesIO(data))
                image_np = np.array(image)  # Convert PIL image to NumPy array

                if ffmpeg is None:
                    ffmpeg = subprocess.Popen([
                            'ffmpeg',
                            '-y',  # Overwrite output file if it exists
                            '-f', 'rawvideo',  # Input format is raw video
                            '-vcodec', 'rawvideo',  # Video codec for raw input
                            '-pix_fmt', 'bgr24',  # Pixel format (OpenCV uses BGR)
                            '-s', f'{image_np.shape[1]}x{image_np.shape[0]}',  # Frame size
                            '-r', '30',  # Frame rate
                            '-i', '-',  # Input comes from stdin (pipe)
                            '-an',  # Disable audio (if input has no audio stream)
                            '-vcodec', 'rawvideo',  # Use rawvideo codec for the output
                            '-f', 'v4l2',  # Output format is v4l2
                            '-pix_fmt', 'yuv420p',  # Ensure this matches the virtual device requirements
                            '/dev/video2'  # Output to virtual device
                          ], stdin=subprocess.PIPE)
                

                # Display the image using OpenCV
                cv2.imshow("Live Stream", cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR))
                
                # Use a small delay to allow the OpenCV window to refresh
                # This ensures it doesn't depend on user input and updates continuously
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break  # Optional: break loop on 'q' key press to close the window

                
                ffmpeg.stdin.write(cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR).tobytes())
        '''
        if ffmpeg is not None:
            ffmpeg.stdin.close()
            ffmpeg.wait()
            exit()
            '''

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

