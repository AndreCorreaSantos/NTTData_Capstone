FROM continuumio/miniconda3

# Install mamba in the base environment
RUN conda install -n base -c conda-forge mamba -y

# Copy the environment.yaml file into the image
COPY environment.yaml /tmp/environment.yaml

# Clean conda cache
RUN conda clean -a --force

# Create the environment using mamba for faster installation
RUN mamba env create -f /tmp/environment.yaml --prefix /opt/conda/envs/env

# Initialize bash for conda/mamba
RUN conda init bash

# Add the environment activation to bashrc
RUN echo "source activate /opt/conda/envs/env" >> ~/.bashrc

# Set the PATH to include the new environment
ENV PATH /opt/conda/envs/env/bin:$PATH
