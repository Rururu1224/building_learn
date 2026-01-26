import cv2
import numpy as np
import bm3d
import os
from skimage.metrics import peak_signal_noise_ratio as psnr
from skimage.metrics import structural_similarity as ssim

# 1. 路径设置
base_path = r'D:\AAAlyk\experiment\exp1'
img_path = os.path.join(base_path, 'data', 'lenna.png') # 确保 data 文件夹里有这张图
save_path = os.path.join(base_path, 'result')

if not os.path.exists(save_path):
    os.makedirs(save_path)

# 2. 读取原图
img = cv2.imread(img_path, cv2.IMREAD_GRAYSCALE)
if img is None:
    print(f"错误：找不到图片 {img_path}")
    exit()

# 保存一份原图到结果文件夹
cv2.imwrite(os.path.join(save_path, '0_original.png'), img)

# 3. 定义两种噪声处理流程
# 噪声类型列表：(名称, 噪声添加函数, 参数)
def add_gaussian_noise(image, sigma=25):
    noise = np.random.normal(0, sigma, image.shape)
    return np.clip(image.astype(np.float32) + noise, 0, 255).astype(np.uint8), sigma/255

def add_salt_pepper_noise(image, prob=0.05):
    noisy = image.copy()
    thres = 1 - prob
    for i in range(image.shape[0]):
        for j in range(image.shape[1]):
            rdn = np.random.random()
            if rdn < prob: noisy[i][j] = 0
            elif rdn > thres: noisy[i][j] = 255
    return noisy, 0.1 # 椒盐噪声去噪时 sigma_psd 设为一个经验值(如0.1)

tasks = [
    ('gaussian', add_gaussian_noise),
    ('salt_pepper', add_salt_pepper_noise)
]

# 4. 循环执行实验 
for name, noise_func in tasks:
    print(f"\n正在处理 {name} 噪声...")
    
    # 添加噪声
    noisy_img, sigma_psd = noise_func(img)
    
    # BM3D 去噪 [cite: 5]
    # 注意：新版本 bm3d 库使用 sigma_psd 参数
    denoised_img = bm3d.bm3d(noisy_img, sigma_psd=sigma_psd)
    denoised_img = (denoised_img * 255).clip(0, 255).astype(np.uint8)
    
    # 计算指标 
    p = psnr(img, denoised_img)
    s = ssim(img, denoised_img)
    print(f"{name} 结果 -> PSNR: {p:.2f} dB, SSIM: {s:.4f}")
    
    # 保存图片 [cite: 11]
    cv2.imwrite(os.path.join(save_path, f'{name}_1_noisy.png'), noisy_img)
    cv2.imwrite(os.path.join(save_path, f'{name}_2_denoised.png'), denoised_img)
