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
from ultralytics import YOLO

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

def get_classes_from_prompt(chat):
    prompt = ChatPromptTemplate.from_messages([
            ("system", """
                Your task is to aid in the selection of the adequet classes for a vision detection program. 
                You will recieve a brief description of what the user wants to detect, and what is relevant to him
                You will only return a list with the contents of the list
                You must only respond with classes found in the following list of classes:
                {classes}
             """),
            ("user", "{input}")
            ])

    input_str = input("Describe what you want YOLO to identify on the scene.")
    classes = YOLO('yolov9c.pt').names

    output_parser = StrOutputParser()
    chain = prompt | chat | output_parser
    response = chain.invoke(
            {
                "classes":json.dumps(classes),
                "input":input_str
            })
    return response


if __name__ == "__main__":
    chat = AzureChatOpenAI(
        deployment_name="grad-eng",  # Your deployment name
        openai_api_version="2023-03-15-preview",
        model="gpt-4o"
    )
    #run_analyzer(chat)
    print(get_classes_from_prompt(chat))
