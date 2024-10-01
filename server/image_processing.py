import cv2
import numpy as np
import json
import colorsys

def quaternion_to_rotation_matrix(qx, qy, qz, qw):
    return np.array([
        [1 - 2*(qy**2 + qz**2),     2*(qx*qy - qz*qw),     2*(qx*qz + qy*qw)],
        [2*(qx*qy + qz*qw),     1 - 2*(qx**2 + qz**2),     2*(qy*qz - qx*qw)],
        [2*(qx*qz - qy*qw),         2*(qy*qz + qx*qw),     1 - 2*(qx**2 + qy**2)]
    ])

def process_image(current_frame, depth_image, detection, rotation, position, fx, fy, cx, cy):
    try:
        box = detection.get('box')
        if not box:
            print("No bounding box found in detection.")
            return None

        box_values = [int(v) for v in box.values()]
        cv2.rectangle(current_frame, (box_values[0], box_values[1]), (box_values[2], box_values[3]), (0, 255, 0), 2)

        # Compute the center of the bounding box
        x = (box_values[0] + box_values[2]) / 2.0
        y = (box_values[1] + box_values[3]) / 2.0
        cv2.circle(current_frame, (int(x), int(y)), 5, (0, 0, 255), -1)

        height,width,channels = current_frame.shape

        x = width - x
        y = height - y
        
        # Extract the depth at the bounding box center
        depth = np.mean(depth_image[box_values[1]:box_values[3], box_values[0]:box_values[2]])
        
        # Convert pixel coordinates to normalized image coordinates
        Zc = depth
        Xc = (x - cx) * Zc / fx
        Yc = (y - cy) * Zc / fy
        P_camera = np.array([Xc, Yc, Zc])

        # Get rotation matrix from quaternion
        qx = rotation['x']
        qy = rotation['y']
        qz = rotation['z']
        qw = rotation['w']
        R = quaternion_to_rotation_matrix(qx, qy, qz, qw)

        # Get camera position
        T = np.array([position['x'], position['y'], position['z']])

        # Transform point from camera coordinates to world coordinates
        P_world = R @ P_camera + T  # If R represents camera-to-world rotation

        object_position = {
            'x': float(P_world[0]),
            'y': float(P_world[1]),
            'z': float(P_world[2])
        }

        print(f"Object position: {object_position}")
        return object_position

    except Exception as e:
        print(f"Error in process_image: {e}")
        return None

import cv2
import numpy as np

def LAB_to_RGB(L, a, b):
    """
    Converts LAB color space to RGB color space.

    Parameters:
    L (float): L component of LAB color space.
    a (float): a component of LAB color space.
    b (float): b component of LAB color space.

    Returns:
    tuple: RGB values as integers in the range [0, 255].

    """

    y = (L + 16) / 116
    x = a / 500 + y
    z = y - b / 200
    f = lambda t: t ** 3 if t > 6/29 else (t - 16 / 116) / 7.787
    x_ = f(x) * 0.95047
    y_ = f(y) * 1.00000
    z_ = f(z) * 1.08883
    r = 3.2406 * x_ - 1.5372 * y_ - 0.4986 * z_
    g = -0.9689 * x_ + 1.8758 * y_ + 0.0415 * z_
    b = 0.0557 * x_ - 0.2040 * y_ + 1.0570 * z_
    r = 12.92 * r if r <= 0.0031308 else 1.055 * r ** (1/2.4) - 0.055
    g = 12.92 * g if g <= 0.0031308 else 1.055 * g ** (1/2.4) - 0.055
    b = 12.92 * b if b <= 0.0031308 else 1.055 * b ** (1/2.4) - 0.055

    return int(r*255), int(g*255), int(b*255)

def relative_luminance(r, g, b):
    """
    Calculates the relative luminance of a color in the RGB color space.

    Parameters:
    r (int): Red component of the color.
    g (int): Green component of the color.
    b (int): Blue component of the color.

    Returns:
    float: Relative luminance of the color.
    """

    r = r / 255
    g = g / 255
    b = b / 255

    r = r / 12.92 if r <= 0.03928 else ((r + 0.055) / 1.055) ** 2.4
    g = g / 12.92 if g <= 0.03928 else ((g + 0.055) / 1.055) ** 2.4
    b = b / 12.92 if b <= 0.03928 else ((b + 0.055) / 1.055) ** 2.4

    return 0.2126 * r + 0.7152 * g + 0.0722 * b

def rgb_to_hsl(r, g, b):
    """Convert RGB to HSL."""
    r /= 255.0
    g /= 255.0
    b /= 255.0
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    return h, l, s

def hsl_to_rgb(h, l, s):
    """Convert HSL to RGB."""
    r, g, b = colorsys.hls_to_rgb(h, l, s)
    return int(r * 255), int(g * 255), int(b * 255)

def check_and_adjust_contrast(gui_back_color, text_color_bgr, target_ratio=4.5):
    """
    Checks the contrast ratio between the background and text color. 
    Adjusts the text color's lightness and saturation, maintaining its hue. 
    If no suitable adjustment is found, adjusts the background color.

    Parameters:
    gui_back_color (tuple): The RGB color of the GUI background (R, G, B).
    text_color_bgr (tuple): The BGR color of the text (B, G, R).
    target_ratio (float): Desired minimum contrast ratio, typically 4.5 or 7.0 for accessibility.

    Returns:
    tuple: The adjusted (background color, text color), with both colors as RGB values.
    """

    def calculate_contrast(lum1, lum2):
        l1 = min(lum1, lum2)
        l2 = max(lum1, lum2)
        return (l2 + 0.05) / (l1 + 0.05)

    # Calculate initial luminance
    rel_l_background = relative_luminance(gui_back_color[0], gui_back_color[1], gui_back_color[2])
    rel_l_text = relative_luminance(text_color_bgr[2], text_color_bgr[1], text_color_bgr[0])

    # Calculate initial contrast ratio
    contrast_ratio = calculate_contrast(rel_l_background, rel_l_text)

    # If contrast is already sufficient, return the current background and text color
    if contrast_ratio >= target_ratio:
        return gui_back_color, text_color_bgr

    # Step 1: Try adjusting the text color's lightness and saturation while keeping its hue
    h, l, s = rgb_to_hsl(text_color_bgr[2], text_color_bgr[1], text_color_bgr[0])

    for lightness_adjustment in range(0, 101, 5):  # Lightness in percentages from 0 to 100
        adjusted_lightness = lightness_adjustment / 100.0
        adjusted_text_color_rgb = hsl_to_rgb(h, adjusted_lightness, s)
        rel_l_text_adjusted = relative_luminance(adjusted_text_color_rgb[0], adjusted_text_color_rgb[1], adjusted_text_color_rgb[2])

        # Recalculate contrast with adjusted luminance
        new_contrast_ratio = calculate_contrast(rel_l_background, rel_l_text_adjusted)

        # If the new contrast ratio meets the requirement, return the original background and adjusted text color
        if new_contrast_ratio >= target_ratio:
            return gui_back_color, adjusted_text_color_rgb

    # Step 2: If no suitable text color adjustment was found, adjust the background color
    for brightness in range(0, 256, 5):  # Brightness from 0 to 255 in steps of 5
        adjusted_back_color = (brightness, brightness, brightness)  # Adjust background brightness
        rel_l_background_adjusted = relative_luminance(adjusted_back_color[0], adjusted_back_color[1], adjusted_back_color[2])

        # Recalculate contrast with the original text color
        new_contrast_ratio = calculate_contrast(rel_l_background_adjusted, rel_l_text)

        # If the new contrast ratio meets the requirement, return the adjusted background and original text color
        if new_contrast_ratio >= target_ratio:
            return adjusted_back_color, text_color_bgr

    # Step 3: If no suitable adjustment is found, return the final adjusted background and text colors
    return gui_back_color, text_color_bgr



def calculate_background_colors(image, UIScreenCorners):
    """
    Calculate the background and text colors for a graphical user interface (GUI) based on the specified corners of the screen.
    
    Parameters:
    - image: A NumPy array representing the input image from which colors will be calculated.
    - UIScreenCorners: A list of dictionaries containing the screen corner coordinates with 'x' and 'y' keys.

    Returns:
    - gui_back_color: A tuple representing the calculated background color in BGR format.
    - gui_text_color: A tuple representing the calculated text color in BGR format.
    - exterior_roi: A NumPy array representing the external region of interest around the screen corners.
    """
    image = cv2.flip(image, -1)
    # Round and extract the coordinates of the screen corners
    UIScreenCorners = [
        [(UIScreenCorners[0]['x']), (UIScreenCorners[0]['y'])],
        [(UIScreenCorners[1]['x']), (UIScreenCorners[1]['y'])],
        [(UIScreenCorners[2]['x']), (UIScreenCorners[2]['y'])],
        [(UIScreenCorners[3]['x']), (UIScreenCorners[3]['y'])]
    ]

    print(f"UIScreenCorners: {UIScreenCorners}")

    height, width = image.shape[:2]
    
    # Calculate the minimum and maximum x and y coordinates to define the region of interest (ROI)
    min_x = min(UIScreenCorners[0][0], UIScreenCorners[1][0], UIScreenCorners[2][0], UIScreenCorners[3][0])
    max_x = max(UIScreenCorners[0][0], UIScreenCorners[1][0], UIScreenCorners[2][0], UIScreenCorners[3][0])
    min_y = min(UIScreenCorners[0][1], UIScreenCorners[1][1], UIScreenCorners[2][1], UIScreenCorners[3][1])
    max_y = max(UIScreenCorners[0][1], UIScreenCorners[1][1], UIScreenCorners[2][1], UIScreenCorners[3][1])

    print(f"min_x: {min_x*width}, max_x: {max_x*width}, min_y: {min_y*height}, max_y: {max_y*height}")
    print(f"width: {width}, height: {height}")

    min_x  = int(np.round(min_x*width))
    max_x  = int(np.round(max_x*width))
    min_y  = int(np.round(min_y*height))
    max_y  = int(np.round(max_y*height))

    print(f"min_x: {min_x}, max_x: {max_x}, min_y: {min_y}, max_y: {max_y}")
    
    # Define the interior region of interest (ROI) based on the calculated corners
    interior_min_x = max(0, min_x)
    interior_max_x = min(width, max_x)
    interior_min_y = max(0, min_y)
    interior_max_y = min(height, max_y)
    interior_roi = image[interior_min_y:interior_max_y, interior_min_x:interior_max_x]
    
    # Define an offset for the exterior ROI to capture surrounding areas
    interior_roi_offset = 0.05

    # Calculate the exterior ROI, clamping values to the image dimensions
    exterior_min_y = max(0, int(min_y - interior_roi_offset * height))
    exterior_max_y = min(height, int(max_y + interior_roi_offset * height))
    exterior_min_x = max(0, int(min_x - interior_roi_offset * width))
    exterior_max_x = min(width, int(max_x + interior_roi_offset * width))

    # Extract the exterior ROI
    exterior_roi = image[exterior_min_y:exterior_max_y, exterior_min_x:exterior_max_x]
    
    # Calculate the mean colors for the interior and exterior ROIs
    #interior_mean_color_bgr = np.array(cv2.mean(interior_roi[:3])[:3])
    exterior_mean_color_bgr = np.array(cv2.mean(exterior_roi[:3])[:3])

    # Reshape the mean color to create a valid image for cv2.cvtColor
    exterior_mean_color_bgr = np.reshape(exterior_mean_color_bgr, (1, 1, 3)).astype(np.uint8)

    # Convert the exterior mean color from BGR to LAB color space
    mean_color_lab = cv2.cvtColor(exterior_mean_color_bgr, cv2.COLOR_BGR2LAB)
    L, a, b = mean_color_lab[0, 0]

    # Scale L for further processing
    L_scaled = L * (100 / 255)

    delta_L = 27
    # Adjust the lightness based on its current value
    if L_scaled > 50:
        L_new = max(0, L_scaled - delta_L)
    else:
        # If L_mean is less than delta_L, set background to 0 (black)
        L_new = 0

    # Scale the adjusted L back to the range suitable for LAB
    L_new_scaled = L_new * (255 / 100)
    
    # Create a new LAB color using the adjusted lightness
    background_new_color_lab = np.uint8([[[L_new_scaled, a, b]]])
    
    # Convert the new LAB color back to BGR format
    background_new_color_bgr = cv2.cvtColor(background_new_color_lab, cv2.COLOR_LAB2BGR)

    # Define the GUI background color in BGR format
    gui_back_color = (
        int(background_new_color_bgr[0, 0, 2]),  # R
        int(background_new_color_bgr[0, 0, 1]),  # G
        int(background_new_color_bgr[0, 0, 0])   # B
    )

    # Calculate a lighter color for text, based on white in LAB color space
    # https://www.w3.org/WAI/WCAG21/Techniques/general/G18
    text_color_bgr = gui_back_color #  Use the new color as the base for text color
    gui_back_color, text_color_bgr = check_and_adjust_contrast(gui_back_color, text_color_bgr)

    gui_text_color = (
        int(text_color_bgr[0]),
        int(text_color_bgr[1]),
        int(text_color_bgr[2])
    )

    # Return the calculated GUI background and text colors, along with the ROI
    return gui_back_color, gui_text_color, interior_roi
