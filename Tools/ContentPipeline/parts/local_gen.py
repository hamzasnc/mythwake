"""Generates the shared part prompts (gen_prompts.py) with a local image model on the PC's own GPU.

usage: <venv python> local_gen.py <out dir> <reference png> [--only 1,2,3] [--candidates 2]

Runs on Windows through DirectML (works on AMD and NVIDIA cards; ROCm for Windows computed wrong matrix products
on the team's RX 9070 XT). Model: Animagine XL 4.0 (SDXL anime model, CreativeML Open RAIL++-M: outputs may be used
commercially) with IP-Adapter Plus so the reference picture steers character and style.
Writes <out>/<NN_name>_<k>.png.
"""
import sys
import time
from pathlib import Path

try:  # use the Windows certificate store (a proxy may re-sign HTTPS for the model downloads)
    import truststore
    truststore.inject_into_ssl()
except ImportError:
    pass

import torch
import torch_directml
from diffusers import StableDiffusionXLPipeline
from PIL import Image
from transformers import CLIPVisionModelWithProjection

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gen_prompts import PROMPTS, STYLE  # noqa: E402

MODEL = "cagliostrolab/animagine-xl-4.0"
SIZES = {(1, 1): (1024, 1024), (2, 3): (832, 1216), (1, 2): (704, 1408), (3, 4): (896, 1152), (4, 3): (1152, 896),
         (2, 1): (1408, 704)}
QUALITY = "masterpiece, best quality, "  # the model's usual quality tags, put in front of the shared prompt
NEGATIVE = ("lowres, bad anatomy, bad hands, extra fingers, text, watermark, signature, multiple views, character sheet, "
            "background scenery, ground shadow, blurry, jpeg artifacts")


def main():
    out, ref = Path(sys.argv[1]), Image.open(sys.argv[2]).convert("RGB")
    only = {int(x) for x in sys.argv[sys.argv.index("--only") + 1].split(",")} if "--only" in sys.argv else None
    cands = int(sys.argv[sys.argv.index("--candidates") + 1]) if "--candidates" in sys.argv else 2
    out.mkdir(parents=True, exist_ok=True)
    dml = torch_directml.device()
    encoder = CLIPVisionModelWithProjection.from_pretrained("h94/IP-Adapter", subfolder="models/image_encoder",
                                                            torch_dtype=torch.float16)
    pipe = StableDiffusionXLPipeline.from_pretrained(MODEL, torch_dtype=torch.float16, image_encoder=encoder)
    pipe.load_ip_adapter("h94/IP-Adapter", subfolder="sdxl_models", weight_name="ip-adapter-plus_sdxl_vit-h.safetensors")
    pipe.set_ip_adapter_scale(0.55)
    pipe.to(dml)
    pipe.set_progress_bar_config(disable=True)
    for n, name, aspect, text in PROMPTS:
        if only and n not in only:
            continue
        w, h = SIZES[aspect]
        for k in range(cands):
            path = out / f"{n:02d}_{name}_{k}.png"
            if path.exists():
                continue
            t = time.time()
            image = pipe(prompt=QUALITY + text + ", " + STYLE, negative_prompt=NEGATIVE, ip_adapter_image=ref,
                         width=w, height=h, num_inference_steps=28, guidance_scale=5.0,
                         generator=torch.Generator().manual_seed(1000 * n + k)).images[0]
            image.save(path)
            print(f"{path.name} {time.time() - t:.0f}s", flush=True)


if __name__ == "__main__":
    main()
