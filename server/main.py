from fastapi import FastAPI, WebSocket
import uvicorn

app = FastAPI()

@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    print("WebSocket connection starting...")
    await websocket.accept()
    while True:
        try:
            # Receive a message
            message = await websocket.receive()
            data = message["bytes"]
            print(f"Received binary message: {data}")
            await websocket.send_text(f"Received binary data of length {len(data)}")
            

        except KeyError as e:
            print(f"Error receiving message: {e}")
            break
        except Exception as e:
            print(f"Unexpected error: {e}")
            break

if __name__ == "__main__":
    port = 8000
    print(f"Starting server and listening on port {port}...")
    uvicorn.run(app, host="0.0.0.0", port=port)
