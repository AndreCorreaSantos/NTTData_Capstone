import requests
import time
import torch
from PIL import Image, ImageDraw
from transformers import AutoProcessor, AutoModelForZeroShotObjectDetection
import matplotlib.pyplot as plt
import numpy as np
import cv2
import os
from langchain_openai.chat_models import AzureChatOpenAI
from langchain_core.prompts import ChatPromptTemplate, HumanMessagePromptTemplate, SystemMessagePromptTemplate
from langchain_core.output_parsers import StrOutputParser
from langchain.schema import HumanMessage
import base64
import re
import asyncio
from fastapi import WebSocket
import json
#from ultralytics import YOLO

# Set Azure OpenAI environment variables
os.environ["OPENAI_API_TYPE"] = "azure"
os.environ["OPENAI_API_KEY"] = os.environ["AZURE_OPENAI_API_KEY"]
os.environ["OPENAI_API_BASE"] = os.environ["AZURE_OPENAI_ENDPOINT"]
os.environ["OPENAI_API_VERSION"] = "2023-03-15-preview"

def encode_image(image_path):
    with open(image_path, "rb") as image_file:
        return base64.b64encode(image_file.read()).decode('utf-8')

def get_all_images_from_dir(path_to_dir):
    #with locks.file_lock:
    regex = re.compile('.*\.(jpe?g|png)$')
    f_matches = []

    for root, dirs, files in os.walk(path_to_dir):
        for file in files:
            if regex.match(file):
                f_matches.append(file)
    print(f_matches)
    return f_matches

def generate_prompt_from_images(images, sys_prompt, instructions):
    m_intructions = [("system", sys_prompt)]
    for image in images:
        image_data = encode_image(image)
        m_intructions.append(
            HumanMessage(
                    content=[
                        {
                            "type": "image_url",
                            "image_url": {"url": f"data:image/jpeg;base64,{image_data}"},
                        },
                    ],
                )
            )
    m_intructions.append(HumanMessage(content=[{"type": "text", "text": instructions}])),
    return ChatPromptTemplate.from_messages(m_intructions)

def run_analyzer(chat):

    images = get_all_images_from_dir("./")
    image_data = encode_image(images[0])


    
    """
    prompt = ChatPromptTemplate.from_messages([
        ("system", "Analyze the given images for signs of danger, and describe the source of it"),
        HumanMessage(
        content=[
            {"type": "text", "text": "describe this image"},
            {
                "type": "image_url",
                "image_url": {"url": "data:image/jpeg;base64,{image_data}"},
            },
        ],
        )
    ])
    """

    system_prompt = """
                        You must only analyze the images for danger
                        You must consider things like open fires, step hazards, and things of that nature things of IMMEDIATE DANGER
                        Tou must consider things like potential flames, hazardous materials, train tracks, and other potential dangers as POTENTIAL DANGER
                        If you find nothing that can be considered dangerous on the image, you must consider the danger level as LOW DANGER
                        You MUST only respond in the format of a valid json, as formatted below. DO NOT add json to the start of the message:
                        {{
                            "type" : "danger_analysis"
                            "danger_level": "{{level of danger detected on the image}}",
                            "danger_source": "{{the source of danger, if detected. if none are detected, fill with NoDangerSources}}"
                        }}
                    """
    instruction_prompt = "Analyze all the images. Give me a response for each one of the images"

    prompt = generate_prompt_from_images(images, system_prompt, instruction_prompt)

    # Create a human message
    #response = chat.invoke([message])

    #print(response.content)

    print("##################")
    output_parser = StrOutputParser()
    chain = prompt | chat | output_parser
    #response = chain.invoke({"input": message})
    response = chain.invoke({})
    print(response)

def get_classes_from_prompt_dino(chat):
    prompt = ChatPromptTemplate.from_messages([
            ("system", """
                Your task is to aid in the selection of the adequate classes for a vision detection program. 
                You will recieve a brief description of what the user wants to detect, and what is relevant to him
                You will respond only with a sentence, with each object that will be detected.
                For example, the user input is: I am a birdwatcher, and I am on the lookout for birds and other animals that live in the area, such as bears.
                The response will be: A bird, a bear.
                Do not limit yourself to what is on the prompt. Given context, add anything whitch may be relevant to the user.
             """),
            ("user", "{input}")
            ])

    input_str = input("Describe what you want GroundingDINO to identify on the scene. ")

    output_parser = StrOutputParser()
    chain = prompt | chat | output_parser
    response = chain.invoke(
            {
                "input":input_str
            })
    print("Classes to be identified by grounding dino: ", response)
    return response


chat = AzureChatOpenAI(
    deployment_name="grad-eng",  # Your deployment name
    openai_api_version="2023-03-15-preview",
    model="gpt-4o"
)
#run_analyzer(chat)
text = get_classes_from_prompt_dino(chat)
# print("AAAAAA")



model_id = "IDEA-Research/grounding-dino-tiny"
device = "cuda"

processor = AutoProcessor.from_pretrained(model_id)
model = AutoModelForZeroShotObjectDetection.from_pretrained(model_id).to(device)

#image_dogs_url = "https://www.lonetreevet.com/blog/wp-content/uploads/2019/05/iStock-688041916.jpg"
# image_url = "https://t4.ftcdn.net/jpg/03/09/37/33/360_F_309373366_ypYQ67CJREwZC9ma0mw94NYcc55mrdtf.jpg"
image_url = "https://cdn-ckgki.nitrocdn.com/eIjtlqSrzAKXFrsHSjkfXOrmttOUeOlc/assets/images/optimized/rev-69b956e/esub.com/wp-content/uploads/2020/10/shutterstock_1180341814-e1601583379631.jpg"
#image_url = "https://www.sociallifeproject.org/content/images/2022/05/Democratize-Streets-as-Places-for-People-1-1.jpeg"
image = Image.open(requests.get(image_url, stream=True).raw)
# Check for cats and remote controls
#text = "A car. A pedestrian"

inputs = processor(images=image, text=text, return_tensors="pt").to(device)
start_time = time.time()
with torch.no_grad():
    outputs = model(**inputs)

end_time = time.time()

print(f"Time taken: {end_time - start_time}")

results = processor.post_process_grounded_object_detection(
    outputs,
    inputs.input_ids,
    box_threshold=0.3,
    text_threshold=0.3,
    target_sizes=[image.size[::-1]]
)

image = np.array(image)  # Uncomment if image is still a PIL image
image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

print(f"model results{results}")
for result in results:
    boxes = result["boxes"].cpu().tolist()  # Convert tensor to list and move to CPU
    labels = result["labels"]
    # Draw the rectangle for each box
    for i in range(len(boxes)):
        box = boxes[i]
        label = labels[i]
        # OpenCV uses (x1, y1, x2, y2) for rectangle
        cv2.rectangle(image, (int(box[0]), int(box[1])), (int(box[2]), int(box[3])), (0, 255, 0), 2)
        cv2.putText(image, label,(int(box[0]), int(box[1] - 10)),0,0.3,(0,255,0))

# Display the image using OpenCV
cv2.imshow('Image', image)
cv2.waitKey(0)