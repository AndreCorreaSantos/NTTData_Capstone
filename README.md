# Project Setup Instructions

### Step 1: Install Miniforge

Install Miniforge from the following link:

- [Miniforge Installation](https://github.com/conda-forge/miniforge)

Miniforge provides a minimal conda installer for using the `conda-forge` package manager.



### Step 2: Create the Mamba Environment

Once Miniforge is installed, create the environment using the provided `environment.yaml` file (must be in the server directory)):

```
mamba env create --file environment.yaml
```

### Step 3: Download Model Checkpoint

Download the required model checkpoint:

```
curl -L -o depth_anything_v2_metric_hypersim_vitb.pth "https://huggingface.co/depth-anything/Depth-Anything-V2-Metric-Hypersim-Base/resolve/main/depth_anything_v2_metric_hypersim_vitb.pth?download=true"
```

---

<details>
  <summary>Or Install Manually</summary>

If you'd prefer to set up the environment manually, follow these steps:

### Step 1: Create and Activate the Environment

```
mamba create -n ntt-pfe
mamba activate ntt-pfe
```

### Step 2: Install PyTorch and CUDA Support

```
mamba install pytorch torchvision torchaudio pytorch-cuda=11.8 -c pytorch -c nvidia
```

### Step 3: Install Python Dependencies

```
pip install fastapi ultralytics transformers aiofiles uvicorn
```

### Step 4: Download Model Checkpoint

Download the model checkpoint manually:

```
curl -L -o depth_anything_v2_metric_hypersim_vitb.pth "https://huggingface.co/depth-anything/Depth-Anything-V2-Metric-Hypersim-Base/resolve/main/depth_anything_v2_metric_hypersim_vitb.pth?download=true"
```

</details>

---
