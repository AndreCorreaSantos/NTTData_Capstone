from openai import AzureOpenAI
import base64
import os
import re
import asyncio
<<<<<<< HEAD
import locks
from fastapi import WebSocket
import json
=======
import cv2
>>>>>>> main

# Function to encode the image
def encode_image(image_path):
    with open(image_path, "rb") as image_file:
        return base64.b64encode(image_file.read()).decode('utf-8')


<<<<<<< HEAD
async def analyze_image(image_path, azure_client, websocket: WebSocket | None):
=======
def analyze_image(image_path, azure_client):
>>>>>>> main
    base64_image = encode_image(image_path)

    completion = azure_client.chat.completions.create(
        model="grad-eng",
        messages=[
            {"role": "system", "content": "You are a helpful assistant."},
            {
                "role": "user",
                "content": [
                    {
                        "type": "text",
                        "text": """
                            You must only analyze the image for danger
                            You must consider things like open fires, step hazards, and things of that nature things of IMMEDIATE DANGER
                            Tou must consider things like potential flames, hazardous materials, train tracks, and other potential dangers as POTENTIAL DANGER
                            If you find nothing that can be considered dangerous on the image, you must consider the danger level as LOW DANGER
<<<<<<< HEAD
                            You MUST only respond in the format of a valid json, as formatted below. DO NOT add json to the start of the message:
                            {
                                "type" : "danger_analysis"
                                "danger_level": "{level of danger detected on the image}",
                                "danger_source": "{the source of danger, if detected. if none are detected, fill with NoDangerSources}"
=======
                            You MUST only respond in the following format:
                            {
                                DangerLevel: [level of danger detected on the image]
                                DangerSource: [the source of danger, if detected. if none are detected, fill with NoDangerSources]
>>>>>>> main
                            }


                            Analyze the potential dangers of this image
                            """
                    },
                    {
                        "type": "image_url",
                        "image_url": {
                            "url": f"data:image/jpeg;base64,{base64_image}"
                        }
                    }
                ]
            }
        ]
    )
<<<<<<< HEAD
    message = completion.choices[0].message.content
    print(message)
    
    if websocket is not None:
        print("sending to websocket")
        try:
            async with locks.websocket_lock:
                await websocket.send_text(message)
        except Exception as e:
            print(f"Error: {e}")



async def get_all_images_from_dir(path_to_dir):
    async with locks.file_lock:
        regex = re.compile('.*\.(jpe?g|png)$')
        f_matches = []

        for root, dirs, files in os.walk(path_to_dir):
            for file in files:
                if regex.match(file):
                    f_matches.append(file)
        return f_matches


async def analyze_all_images_in_dir(path_to_dir, client, websocket: WebSocket | None):
    images = await get_all_images_from_dir(path_to_dir)
    for image in images:
        await analyze_image(path_to_dir + image, client, websocket)


async def run_analyzer(websocket: WebSocket | None):

    azure_client = AzureOpenAI(
=======
    print(completion.choices[0].message)


def get_all_images_from_dir(path_to_dir):
    regex = re.compile('.*\.(jpe?g|png)$')
    f_matches = []

    for root, dirs, files in os.walk(path_to_dir):
        for file in files:
            if regex.match(file):
                f_matches.append(file)
    return f_matches


def analyze_all_images_in_dir(path_to_dir, client):
    images = get_all_images_from_dir(path_to_dir)
    for image in images:
        analyze_image(path_to_dir + image, client)


async def run_analyzer():

    azure_client = AzureOpenAI(
        azure_endpoint=os.getenv("AZURE_OPENAI_ENDPOINT"),
>>>>>>> main
        api_version="2023-03-15-preview"
    )

    while True:
        await asyncio.sleep(6)
<<<<<<< HEAD
        await analyze_all_images_in_dir("./gpt/", azure_client, websocket)

if __name__ == "__main__":
    asyncio.run(run_analyzer(None))
=======
        analyze_all_images_in_dir("./gpt/", azure_client)

async def save_captured_image(image, output_name):
    cv2.imwrite(output_name, image, [int(cv2.IMWRITE_WEBP_QUALITY), 60])
>>>>>>> main
