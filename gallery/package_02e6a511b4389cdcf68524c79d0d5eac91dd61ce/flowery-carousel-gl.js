(function () {
    "use strict";

    const DOCUMENT_BASE = new URL(".", document.baseURI).toString();
    const FRAMEWORK_BASE = new URL("_framework/", DOCUMENT_BASE).toString();
    const DEFAULT_OVERLAY_Z_INDEX = 1000;

    function getOverlayZIndex() {
        const raw = window.getComputedStyle(document.documentElement)
            .getPropertyValue("--flowery-carousel-gl-zindex");
        const parsed = Number.parseInt(raw, 10);
        if (Number.isFinite(parsed)) {
            return parsed;
        }

        const popupRoot = document.getElementById("uno-popup-root");
        if (popupRoot) {
            const popupZ = Number.parseInt(window.getComputedStyle(popupRoot).zIndex, 10);
            if (Number.isFinite(popupZ)) {
                return Math.max(1, popupZ - 1);
            }
        }

        return DEFAULT_OVERLAY_Z_INDEX;
    }

    function normalizeTransitionSeconds(value, fallback) {
        const seconds = Number(value);
        if (Number.isFinite(seconds) && seconds > 0) {
            return seconds;
        }
        return fallback;
    }

    function resolveAssetPath(path) {
        if (/^(https?:)?\/\//.test(path) || path.startsWith("data:")) {
            return path;
        }
        const cleaned = path.startsWith("/") ? path.slice(1) : path;
        return new URL(cleaned, FRAMEWORK_BASE).toString();
    }

    const DEFAULT_IMAGES = {};

    const MASK_VARIANTS = {
        "checkerboard": { pattern: 0, tiles: 8 },
        "blinds-h": { pattern: 1, tiles: 10 },
        "blinds-v": { pattern: 2, tiles: 10 },
        "slices-h": { pattern: 3, tiles: 12 },
        "slices-v": { pattern: 4, tiles: 12 },
        "spiral": { pattern: 5, tiles: 8 },
        "matrix": { pattern: 6, tiles: 16 },
        "wormhole": { pattern: 7, tiles: 8 },
        "dissolve": { pattern: 8, tiles: 12 },
        "pixelate": { pattern: 9, tiles: 10 }
    };

    const FLIP_VARIANTS = {
        "flip-plane": { axis: 0, pivotX: 0, pivotY: 0, depth: 2.2, flipAxis: 0 },
        "flip-v": { axis: 0, pivotX: 0, pivotY: 0, depth: 2.2, flipAxis: 0 },
        "flip-h": { axis: 1, pivotX: 0, pivotY: 0, depth: 2.2, flipAxis: 1 },
        "cube-left": { axis: 0, pivotX: -1, pivotY: 0, depth: 1.8, flipAxis: 0, minAngle: 180 },
        "cube-right": { axis: 0, pivotX: 1, pivotY: 0, depth: 1.8, flipAxis: 0, minAngle: 180 }
    };

    const TRANSITION_VARIANTS = {
        "fade": { type: 0, dir: [0, 0] },
        "fade-black": { type: 1, dir: [0, 0] },
        "fade-white": { type: 2, dir: [0, 0] },
        "slide-left": { type: 3, dir: [-1, 0] },
        "slide-right": { type: 3, dir: [1, 0] },
        "slide-up": { type: 3, dir: [0, -1] },
        "slide-down": { type: 3, dir: [0, 1] },
        "push-left": { type: 4, dir: [-1, 0] },
        "push-right": { type: 4, dir: [1, 0] },
        "push-up": { type: 4, dir: [0, -1] },
        "push-down": { type: 4, dir: [0, 1] },
        "cover-left": { type: 5, dir: [-1, 0] },
        "cover-right": { type: 5, dir: [1, 0] },
        "reveal-left": { type: 6, dir: [-1, 0] },
        "reveal-right": { type: 6, dir: [1, 0] },
        "wipe-left": { type: 7, dir: [-1, 0] },
        "wipe-right": { type: 7, dir: [1, 0] },
        "wipe-up": { type: 7, dir: [0, -1] },
        "wipe-down": { type: 7, dir: [0, 1] },
        "zoom-in": { type: 8, dir: [0, 0] },
        "zoom-out": { type: 9, dir: [0, 0] },
        "zoom-cross": { type: 10, dir: [0, 0] }
    };

    const TRANSITION_VARIANT_KEYS = Object.keys(TRANSITION_VARIANTS);

    const EFFECT_VARIANTS = {
        "pan-zoom": { type: "pan-zoom" },
        "zoom-in": { type: "zoom-in" },
        "zoom-out": { type: "zoom-out" },
        "pan-left": { type: "pan-left" },
        "pan-right": { type: "pan-right" },
        "pan-up": { type: "pan-up" },
        "pan-down": { type: "pan-down" },
        "drift": { type: "drift" },
        "pulse": { type: "pulse" },
        "breath": { type: "breath" },
        "throw": { type: "throw" }
    };

    function resolveImages(mode, assets) {
        const merged = Object.assign({}, DEFAULT_IMAGES, assets || {});
        if (mode === "mask") {
            return { maskA: merged.maskA, maskB: merged.maskB };
        }
        if (mode === "flip") {
            return { flipA: merged.flipA, flipB: merged.flipB };
        }
        if (mode === "transition") {
            return { transitionA: merged.transitionA, transitionB: merged.transitionB };
        }
        if (mode === "effect") {
            return { effect: merged.effect };
        }
        if (mode === "text") {
            return { text: merged.text };
        }
        return merged;
    }

    function getPairKeys(mode) {
        if (mode === "mask") {
            return { a: "maskA", b: "maskB" };
        }
        if (mode === "flip") {
            return { a: "flipA", b: "flipB" };
        }
        if (mode === "transition") {
            return { a: "transitionA", b: "transitionB" };
        }
        return null;
    }

    function resolveAssetsForCurrentImage(mode, assets, state) {
        const pair = getPairKeys(mode);
        if (!pair || !assets || !state) {
            return assets;
        }

        const assetA = assets[pair.a];
        const assetB = assets[pair.b];
        if (!assetA || !assetB || assetA === assetB) {
            return assets;
        }

        const currentImage = state.currentImage;
        let shouldSwap = false;
        if (currentImage) {
            if (currentImage === assetB && currentImage !== assetA) {
                shouldSwap = true;
            }
        } else if (state.currentSwap === true) {
            shouldSwap = true;
        }

        if (!shouldSwap) {
            return assets;
        }

        const swapped = Object.assign({}, assets);
        swapped[pair.a] = assetB;
        swapped[pair.b] = assetA;
        return swapped;
    }

    function getMaskSettings(variant) {
        if (variant && MASK_VARIANTS[variant]) {
            return MASK_VARIANTS[variant];
        }
        return MASK_VARIANTS.checkerboard;
    }

    function getFlipSettings(variant) {
        if (variant && FLIP_VARIANTS[variant]) {
            return FLIP_VARIANTS[variant];
        }
        return FLIP_VARIANTS["flip-plane"];
    }

    function getTransitionSettings(variant) {
        if (variant === "random" && TRANSITION_VARIANT_KEYS.length > 0) {
            const choice = TRANSITION_VARIANT_KEYS[Math.floor(Math.random() * TRANSITION_VARIANT_KEYS.length)];
            return TRANSITION_VARIANTS[choice];
        }
        if (variant && TRANSITION_VARIANTS[variant]) {
            return TRANSITION_VARIANTS[variant];
        }
        return TRANSITION_VARIANTS.fade;
    }

    function getEffectSettings(variant) {
        if (variant && EFFECT_VARIANTS[variant]) {
            return EFFECT_VARIANTS[variant];
        }
        return EFFECT_VARIANTS["pan-zoom"];
    }

    function getEffectParams(type, timeSec) {
        let offsetX = 0;
        let offsetY = 0;
        let scale = 1;
        let rotation = 0;
        let alpha = 1;
        const t = timeSec;

        switch (type) {
            case "zoom-in": {
                const pulse = 0.5 - 0.5 * Math.cos(t * 0.6);
                scale = 1.0 + 0.18 * pulse;
                break;
            }
            case "zoom-out": {
                const pulse = 0.5 - 0.5 * Math.cos(t * 0.6);
                scale = 1.18 - 0.18 * pulse;
                break;
            }
            case "pan-left":
                scale = 1.12;
                offsetX = -0.12 + 0.03 * Math.sin(t * 0.5);
                offsetY = 0.02 * Math.cos(t * 0.4);
                break;
            case "pan-right":
                scale = 1.12;
                offsetX = 0.12 + 0.03 * Math.sin(t * 0.5);
                offsetY = 0.02 * Math.cos(t * 0.4);
                break;
            case "pan-up":
                scale = 1.12;
                offsetY = -0.12 + 0.03 * Math.cos(t * 0.5);
                offsetX = 0.02 * Math.sin(t * 0.4);
                break;
            case "pan-down":
                scale = 1.12;
                offsetY = 0.12 + 0.03 * Math.cos(t * 0.5);
                offsetX = 0.02 * Math.sin(t * 0.4);
                break;
            case "drift":
                scale = 1.08;
                offsetX = 0.06 * Math.sin(t * 0.25);
                offsetY = 0.06 * Math.cos(t * 0.22);
                rotation = 0.05 * Math.sin(t * 0.18);
                break;
            case "pulse":
                scale = 1.0 + 0.08 * Math.sin(t * 1.2);
                alpha = 0.9 + 0.1 * Math.sin(t * 1.2);
                break;
            case "breath":
                scale = 1.0 + 0.06 * Math.sin(t * 0.6);
                alpha = 0.92 + 0.08 * Math.sin(t * 0.6);
                break;
            case "throw": {
                const phase = (t * 0.25) % 1;
                const ease = 0.5 - 0.5 * Math.cos(phase * Math.PI * 2);
                scale = 1.1;
                offsetX = -0.15 + 0.3 * ease;
                offsetY = 0.12 - 0.24 * ease;
                rotation = -0.12 + 0.24 * ease;
                break;
            }
            case "pan-zoom":
            default:
                scale = 1.12 + 0.06 * Math.sin(t * 0.35);
                offsetX = 0.08 * Math.sin(t * 0.25);
                offsetY = 0.06 * Math.cos(t * 0.2);
                break;
        }

        scale = Math.max(0.2, scale);
        alpha = Math.min(1, Math.max(0, alpha));

        return { offsetX, offsetY, scale, rotation, alpha };
    }

    function loadImage(src) {
        const resolved = resolveAssetPath(src);

        return new Promise((resolve, reject) => {
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = () => resolve(img);
            img.onerror = () => {
                reject(new Error("Failed to load image: " + resolved));
            };
            img.src = resolved;
        });
    }

    function createShader(gl, type, source) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            const info = gl.getShaderInfoLog(shader) || "Shader compile failed";
            gl.deleteShader(shader);
            throw new Error(info);
        }
        return shader;
    }

    function createProgram(gl, vsSource, fsSource) {
        const vs = createShader(gl, gl.VERTEX_SHADER, vsSource);
        const fs = createShader(gl, gl.FRAGMENT_SHADER, fsSource);
        const program = gl.createProgram();
        gl.attachShader(program, vs);
        gl.attachShader(program, fs);
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
            const info = gl.getProgramInfoLog(program) || "Program link failed";
            gl.deleteProgram(program);
            throw new Error(info);
        }
        return program;
    }

    function createTexture(gl, image) {
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
        return texture;
    }

    function createQuad(gl, program) {
        const posLoc = gl.getAttribLocation(program, "a_position");
        const uvLoc = gl.getAttribLocation(program, "a_texCoord");

        const posBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, posBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
            -1, -1,
             1, -1,
            -1,  1,
             1,  1
        ]), gl.STATIC_DRAW);

        const uvBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, uvBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
            0, 0,
            1, 0,
            0, 1,
            1, 1
        ]), gl.STATIC_DRAW);

        return function bind() {
            gl.bindBuffer(gl.ARRAY_BUFFER, posBuffer);
            gl.enableVertexAttribArray(posLoc);
            gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, uvBuffer);
            gl.enableVertexAttribArray(uvLoc);
            gl.vertexAttribPointer(uvLoc, 2, gl.FLOAT, false, 0, 0);
        };
    }

    function getCanvasDisplaySize(canvas) {
        const rect = canvas.getBoundingClientRect();
        const style = window.getComputedStyle(canvas);
        const styleWidth = parseFloat(style.width) || 0;
        const styleHeight = parseFloat(style.height) || 0;
        const width = Math.max(rect.width || 0, canvas.clientWidth || 0, canvas.offsetWidth || 0, styleWidth);
        const height = Math.max(rect.height || 0, canvas.clientHeight || 0, canvas.offsetHeight || 0, styleHeight);
        return { width, height };
    }

    function resizeCanvasToDisplaySize(canvas, gl) {
        const dpr = window.devicePixelRatio || 1;
        const size = getCanvasDisplaySize(canvas);
        const width = Math.max(1, Math.floor(size.width * dpr));
        const height = Math.max(1, Math.floor(size.height * dpr));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
            if (gl) {
                gl.viewport(0, 0, width, height);
            }
            return true;
        }
        return false;
    }

    function drawFallback(canvas, message) {
        const ctx = canvas.getContext("2d");
        if (!ctx) {
            return;
        }
        resizeCanvasToDisplaySize(canvas, null);
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = "#666";
        ctx.font = "16px sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(message, canvas.width / 2, canvas.height / 2);
    }

    function startLoop(state, render) {
        state._render = render;
        if (!state._frame) {
            state._frame = function frame(time) {
                if (!state.running) {
                    return;
                }
                state._render(time);
                state.raf = requestAnimationFrame(state._frame);
            };
        }
        resumeState(state);
    }

    function computeTransitionProgress(elapsedSec, transitionSec, intervalSec) {
        const holdSec = intervalSec > 0 ? intervalSec : 0;
        const transition = Math.max(0.001, transitionSec);
        const cycle = holdSec + transition + holdSec + transition;
        if (cycle <= 0) {
            return 0;
        }
        const t = elapsedSec % cycle;
        if (t < holdSec) {
            return 0;
        }
        if (t < holdSec + transition) {
            return (t - holdSec) / transition;
        }
        if (t < holdSec + transition + holdSec) {
            return 1;
        }
        return 1 - (t - (holdSec + transition + holdSec)) / transition;
    }

    function resolvePixelateSize(value) {
        const size = Number(value);
        return Number.isFinite(size) && size > 0 ? size : 20;
    }

    function resolveSliceCount(value) {
        const count = Number(value);
        if (!Number.isFinite(count)) {
            return 0;
        }
        const rounded = Math.floor(count);
        return rounded >= 2 ? rounded : 0;
    }

    function resolveStaggerFlag(value) {
        return value === true || value === "true" || value === 1;
    }

    function resolveStaggerMs(value) {
        const ms = Number(value);
        return Number.isFinite(ms) && ms > 0 ? ms : 0;
    }

    function resolveDissolveDensity(value) {
        const density = Number(value);
        if (!Number.isFinite(density)) {
            return 0.5;
        }
        return Math.min(1, Math.max(0, density));
    }

    function resolveFlipAngle(value) {
        const angle = Number(value);
        if (!Number.isFinite(angle)) {
            return 180;
        }
        return Math.min(180, Math.max(30, angle));
    }

    function getPixelateBlocks(canvas, pixelateSize) {
        const display = getCanvasDisplaySize(canvas);
        const displayWidth = Math.max(1, display.width);
        const dpr = Math.max(1, canvas.width / displayWidth);
        const minSidePx = Math.max(1, Math.min(canvas.width, canvas.height));
        const pixelSizePx = Math.max(1, pixelateSize * dpr);
        const minBlocks = Math.max(2, Math.floor(minSidePx / pixelSizePx));
        const maxBlocks = Math.max(minBlocks + 1, Math.floor(minSidePx / Math.max(1, pixelSizePx * 0.25)));
        return { minBlocks, maxBlocks };
    }

    function getPixelateTransitionSeconds(state, minBlocks) {
        const base = normalizeTransitionSeconds(state.baseTransitionSec, state.defaultTransitionSec);
        const referenceBlocks = 12;
        const scale = Math.min(5.0, Math.max(1.6, minBlocks / referenceBlocks));
        return base * scale;
    }

    function getSpiralWormholeTransitionSeconds(state) {
        const base = normalizeTransitionSeconds(state.baseTransitionSec, state.defaultTransitionSec);
        return base * 1.25;
    }

    function pauseState(state) {
        if (!state.running) {
            return;
        }
        state.running = false;
        if (state.raf) {
            cancelAnimationFrame(state.raf);
            state.raf = 0;
        }
    }

    function resumeState(state) {
        if (state.running && state.raf) {
            return;
        }
        state.running = true;
        if (state._frame) {
            state.raf = requestAnimationFrame(state._frame);
        }
    }

    function initMask(
        canvas,
        assets,
        variant,
        effectVariant,
        transitionSec,
        sliceCount,
        stagger,
        staggerMs,
        pixelateSize,
        dissolveDensity) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const vs = [
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  v_texCoord = a_texCoord;",
            "  gl_Position = vec4(a_position, 0.0, 1.0);",
            "}"
        ].join("\n");

        const fs = [
            "precision mediump float;",
            "uniform sampler2D u_tex1;",
            "uniform sampler2D u_tex2;",
            "uniform float u_progress;",
            "uniform float u_seed;",
            "uniform float u_tiles;",
            "uniform float u_pattern;",
            "uniform vec2 u_effectOffset;",
            "uniform float u_effectScale;",
            "uniform float u_effectRotation;",
            "uniform float u_effectAlpha;",
            "uniform float u_blockMin;",
            "uniform float u_blockMax;",
            "uniform float u_stagger;",
            "uniform float u_dissolveDensity;",
            "varying vec2 v_texCoord;",
            "float rand(vec2 co) {",
            "  vec2 seeded = co + vec2(u_seed, u_seed * 1.37);",
            "  return fract(sin(dot(seeded, vec2(12.9898, 78.233))) * 43758.5453);",
            "}",
            "float patternMask(vec2 uv, float progress) {",
            "  if (u_pattern < 0.5) {",
            "    vec2 grid = floor(uv * u_tiles);",
            "    float checker = mod(grid.x + grid.y, 2.0);",
            "    float edge = checker * 0.5;",
            "    return smoothstep(edge, edge + 0.5, progress);",
            "  }",
            "  if (u_pattern < 1.5) {",
            "    if (progress <= 0.0) {",
            "      return 0.0;",
            "    }",
            "    float slice = floor(uv.y * u_tiles);",
            "    float delay = rand(vec2(slice, 0.91)) * u_stagger;",
            "    float local = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);",
            "    float stripe = fract(uv.y * u_tiles);",
            "    float edge0 = stripe - 0.08;",
            "    float edge1 = min(stripe + 0.08, 1.0);",
            "    return smoothstep(edge0, edge1, local);",
            "  }",
            "  if (u_pattern < 2.5) {",
            "    if (progress <= 0.0) {",
            "      return 0.0;",
            "    }",
            "    float slice = floor(uv.x * u_tiles);",
            "    float delay = rand(vec2(slice, 0.63)) * u_stagger;",
            "    float local = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);",
            "    float stripe = fract(uv.x * u_tiles);",
            "    float edge0 = stripe - 0.08;",
            "    float edge1 = min(stripe + 0.08, 1.0);",
            "    return smoothstep(edge0, edge1, local);",
            "  }",
            "  if (u_pattern < 3.5) {",
            "    float slice = floor(uv.y * u_tiles);",
            "    float delay = rand(vec2(slice, 0.13)) * u_stagger;",
            "    float local = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);",
            "    float speed = mix(0.5, 1.7, rand(vec2(slice, 1.21)));",
            "    float eased = pow(local, 1.0 / speed);",
            "    float jitter = mix(-0.25, 0.25, rand(vec2(slice, 2.71)));",
            "    float jitterAmt = jitter * (eased * (1.0 - eased) * 2.0);",
            "    float edge = clamp(eased + jitterAmt, 0.0, 1.0);",
            "    return step(uv.x, edge);",
            "  }",
            "  if (u_pattern < 4.5) {",
            "    float slice = floor(uv.x * u_tiles);",
            "    float delay = rand(vec2(slice, 0.37)) * u_stagger;",
            "    float local = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);",
            "    float speed = mix(0.5, 1.7, rand(vec2(slice, 1.57)));",
            "    float eased = pow(local, 1.0 / speed);",
            "    float jitter = mix(-0.25, 0.25, rand(vec2(slice, 2.11)));",
            "    float jitterAmt = jitter * (eased * (1.0 - eased) * 2.0);",
            "    float edge = clamp(eased + jitterAmt, 0.0, 1.0);",
            "    return step(uv.y, edge);",
            "  }",
            "  if (u_pattern < 5.5) {",
            "    if (progress <= 0.0) {",
            "      return 0.0;",
            "    }",
            "    if (progress >= 1.0) {",
            "      return 1.0;",
            "    }",
            "    vec2 p = uv - 0.5;",
            "    float angle = atan(p.y, p.x);",
            "    float angleNorm = (angle + 3.14159265) / 6.2831853;",
            "    float radius = length(p) * 1.2;",
            "    float spiral = angleNorm + radius;",
            "    float t = progress * 2.0;",
            "    float mask = smoothstep(spiral - 0.15, spiral + 0.15, t);",
            "    mask = mix(mask, 1.0, step(0.999, progress));",
            "    mask = mix(mask, 0.0, 1.0 - step(0.001, progress));",
            "    return mask;",
            "  }",
            "  if (u_pattern < 6.5) {",
            "    float col = floor(uv.x * u_tiles);",
            "    float delay = rand(vec2(col, 0.73)) * u_stagger;",
            "    float local = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);",
            "    float fall = 1.0 - uv.y;",
            "    return step(fall, local);",
            "  }",
            "  if (u_pattern < 7.5) {",
            "    vec2 p = uv - 0.5;",
            "    float radius = length(p);",
            "    float angle = atan(p.y, p.x);",
            "    float swirl = radius + 0.12 * sin(angle * 6.0 + radius * 12.0);",
            "    float maxSwirl = 0.827;",
            "    float normalized = clamp(swirl / maxSwirl, 0.0, 1.0);",
            "    float feather = 0.1;",
            "    float mask = smoothstep(normalized - feather, normalized + feather, progress);",
            "    mask = mix(mask, 1.0, step(0.999, progress));",
            "    mask = mix(mask, 0.0, 1.0 - step(0.001, progress));",
            "    return mask;",
            "  }",
            "  if (u_pattern < 8.5) {",
            "    float grain = mix(2.0, 8.0, u_dissolveDensity);",
            "    vec2 noiseUv = floor(uv * u_tiles * grain) + vec2(0.123, 0.456);",
            "    float noise = max(0.001, rand(noiseUv));",
            "    return step(noise, progress);",
            "  }",
            "  return progress;",
            "}",
            "vec2 applyEffect(vec2 uv) {",
            "  vec2 centered = uv - 0.5;",
            "  float c = cos(u_effectRotation);",
            "  float s = sin(u_effectRotation);",
            "  vec2 rot = vec2(centered.x * c - centered.y * s, centered.x * s + centered.y * c);",
            "  return rot / u_effectScale + 0.5 + u_effectOffset;",
            "}",
            "void main() {",
            "  vec2 sampleUv = v_texCoord;",
            "  float pixelPhase = u_progress * 2.0;",
            "  float pixelT = pixelPhase < 1.0 ? pixelPhase : (2.0 - pixelPhase);",
            "  bool isPixelate = u_pattern > 8.5;",
            "  if (isPixelate) {",
            "    float blocks = mix(u_blockMax, u_blockMin, pixelT);",
            "    vec2 pixelUv = floor(sampleUv * blocks) / blocks;",
            "    sampleUv = mix(sampleUv, pixelUv, pixelT);",
            "  }",
            "  vec2 uv1 = applyEffect(sampleUv);",
            "  vec2 uv2 = applyEffect(sampleUv);",
            "  vec4 c1 = texture2D(u_tex1, clamp(uv1, 0.0, 1.0));",
            "  vec4 c2 = texture2D(u_tex2, clamp(uv2, 0.0, 1.0));",
            "  c1.rgb *= u_effectAlpha;",
            "  c2.rgb *= u_effectAlpha;",
            "  c1.a *= u_effectAlpha;",
            "  c2.a *= u_effectAlpha;",
            "  if (isPixelate) {",
            "    float swap = step(1.0, pixelPhase);",
            "    gl_FragColor = mix(c1, c2, swap);",
            "  } else {",
            "    float mask = patternMask(v_texCoord, u_progress);",
            "    gl_FragColor = mix(c1, c2, mask);",
            "  }",
            "}"
        ].join("\n");

        const program = createProgram(gl, vs, fs);
        const bindQuad = createQuad(gl, program);
        const uProgress = gl.getUniformLocation(program, "u_progress");
        const uTiles = gl.getUniformLocation(program, "u_tiles");
        const uPattern = gl.getUniformLocation(program, "u_pattern");
        const uTex1 = gl.getUniformLocation(program, "u_tex1");
        const uTex2 = gl.getUniformLocation(program, "u_tex2");
        const uSeed = gl.getUniformLocation(program, "u_seed");
        const uEffectOffset = gl.getUniformLocation(program, "u_effectOffset");
        const uEffectScale = gl.getUniformLocation(program, "u_effectScale");
        const uEffectRotation = gl.getUniformLocation(program, "u_effectRotation");
        const uEffectAlpha = gl.getUniformLocation(program, "u_effectAlpha");
        const uBlockMin = gl.getUniformLocation(program, "u_blockMin");
        const uBlockMax = gl.getUniformLocation(program, "u_blockMax");
        const uStagger = gl.getUniformLocation(program, "u_stagger");
        const uDissolveDensity = gl.getUniformLocation(program, "u_dissolveDensity");

        const defaultTransitionSec = 2.2;
        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: 0,
            baseTransitionSec: transitionSec,
            transitionSec: normalizeTransitionSeconds(transitionSec, defaultTransitionSec),
            sliceCount: resolveSliceCount(sliceCount),
            stagger: resolveStaggerFlag(stagger),
            staggerMs: resolveStaggerMs(staggerMs),
            pixelateSize: resolvePixelateSize(pixelateSize),
            dissolveDensity: resolveDissolveDensity(dissolveDensity),
            assetA: null,
            assetB: null,
            currentImage: null,
            currentSwap: false,
            defaultTransitionSec,
            seed: Math.random() * 1000,
            seedPhase: -1,
            wormholeStart: 0,
            wormholeSwap: false,
            pause: function () {
                pauseState(state);
            },
            resume: function () {
                resumeState(state);
            },
            stop: function () {
                pauseState(state);
                state._frame = null;
                state._render = null;
            },
            resize: function () {
                resizeCanvasToDisplaySize(canvas, gl);
            }
        };

        const images = resolveImages("mask", assets);
        if (!images.maskA || !images.maskB) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }
        state.assetA = images.maskA;
        state.assetB = images.maskB;
        state.currentImage = images.maskA;
        state.currentSwap = false;
        const settings = getMaskSettings(variant);
        const effectSettings = effectVariant ? getEffectSettings(effectVariant) : null;
        Promise.all([loadImage(images.maskA), loadImage(images.maskB)]).then(([imgA, imgB]) => {
            const texA = createTexture(gl, imgA);
            const texB = createTexture(gl, imgB);

            gl.clearColor(0, 0, 0, 0);
            gl.useProgram(program);
            gl.uniform1i(uTex1, 0);
            gl.uniform1i(uTex2, 1);
            gl.uniform1f(uTiles, state.sliceCount > 1 ? state.sliceCount : settings.tiles);
            gl.uniform1f(uStagger, state.stagger ? 0.5 : 0.0);
            gl.uniform1f(uDissolveDensity, state.dissolveDensity);
            gl.uniform1f(uPattern, settings.pattern);

            const start = performance.now();
            state.wormholeStart = start;
            state.wormholeSwap = false;

            startLoop(state, (time) => {
                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(program);
                bindQuad();

                const elapsedSec = (time - start) * 0.001;
                let transitionSeconds = state.transitionSec;
                const tiles = state.sliceCount > 1 ? state.sliceCount : settings.tiles;
                const staggerFactor = state.stagger && transitionSeconds > 0
                    ? Math.min(0.9, Math.max(0, state.staggerMs / (transitionSeconds * 1000)))
                    : 0;
                let blockMin = 0;
                let blockMax = 0;
                if (settings.pattern > 4.5 && settings.pattern < 5.5) {
                    transitionSeconds = getSpiralWormholeTransitionSeconds(state);
                } else if (settings.pattern > 6.5 && settings.pattern < 7.5) {
                    transitionSeconds = getSpiralWormholeTransitionSeconds(state);
                } else if (settings.pattern > 8.5) {
                    const blocks = getPixelateBlocks(canvas, resolvePixelateSize(state.pixelateSize));
                    blockMin = blocks.minBlocks;
                    blockMax = blocks.maxBlocks;
                    transitionSeconds = getPixelateTransitionSeconds(state, blockMin);
                }
                const isSpiral = settings.pattern > 4.5 && settings.pattern < 5.5;
                const isWormhole = settings.pattern > 6.5 && settings.pattern < 7.5;
                let progress = computeTransitionProgress(elapsedSec, transitionSeconds, state.intervalSec);
                let useSwap = false;
                if (isSpiral || isWormhole) {
                    const holdSec = state.intervalSec > 0 ? state.intervalSec : 0;
                    const transitionSec = Math.max(0.001, transitionSeconds);
                    const cycleSec = holdSec + transitionSec;
                    if (cycleSec > 0) {
                        const elapsedMs = time - state.wormholeStart;
                        const elapsedCycleSec = elapsedMs * 0.001;
                        if (elapsedCycleSec >= cycleSec) {
                            const cycles = Math.floor(elapsedCycleSec / cycleSec);
                            state.wormholeStart += cycles * cycleSec * 1000;
                            if (cycles % 2 === 1) {
                                state.wormholeSwap = !state.wormholeSwap;
                            }
                        }
                        const localSec = Math.max(0, (time - state.wormholeStart) * 0.001);
                        if (localSec < holdSec) {
                            progress = 0;
                        } else {
                            progress = (localSec - holdSec) / transitionSec;
                        }
                        progress = Math.min(1, Math.max(0, progress));
                        useSwap = state.wormholeSwap;
                    }
                }
                const holdSec = state.intervalSec > 0 ? state.intervalSec : 0;
                const transitionSec = Math.max(0.001, transitionSeconds);
                const cycleSec = holdSec + transitionSec + holdSec + transitionSec;
                let phase = 0;
                if (cycleSec > 0) {
                    const t = elapsedSec % cycleSec;
                    if (t < holdSec) {
                        phase = 0;
                    } else if (t < holdSec + transitionSec) {
                        phase = 1;
                    } else if (t < holdSec + transitionSec + holdSec) {
                        phase = 2;
                    } else {
                        phase = 3;
                    }
                }
                if (phase !== state.seedPhase) {
                    state.seedPhase = phase;
                    if (phase === 1 || phase === 3) {
                        state.seed = Math.random() * 1000;
                    }
                }
                if (state.assetA && state.assetB) {
                    const isCurrentB = useSwap ? progress < 0.5 : progress >= 0.5;
                    state.currentSwap = isCurrentB;
                    state.currentImage = isCurrentB ? state.assetB : state.assetA;
                }
                const effectParams = effectSettings ? getEffectParams(effectSettings.type, elapsedSec) : null;
                const effectOffsetX = effectParams ? effectParams.offsetX : 0;
                const effectOffsetY = effectParams ? effectParams.offsetY : 0;
                const effectScale = effectParams ? effectParams.scale : 1;
                const effectRotation = effectParams ? effectParams.rotation : 0;
                const effectAlpha = effectParams ? effectParams.alpha : 1;

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, useSwap ? texB : texA);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, useSwap ? texA : texB);

                gl.uniform1f(uProgress, progress);
                gl.uniform1f(uSeed, state.seed);
                gl.uniform1f(uTiles, tiles);
                gl.uniform2f(uEffectOffset, effectOffsetX, effectOffsetY);
                gl.uniform1f(uEffectScale, effectScale);
                gl.uniform1f(uEffectRotation, effectRotation);
                gl.uniform1f(uEffectAlpha, effectAlpha);
                if (settings.pattern > 8.5) {
                    gl.uniform1f(uBlockMin, blockMin);
                    gl.uniform1f(uBlockMax, blockMax);
                } else {
                    gl.uniform1f(uBlockMin, 1.0);
                    gl.uniform1f(uBlockMax, 1.0);
                }
                gl.uniform1f(uStagger, staggerFactor);
                gl.uniform1f(uDissolveDensity, state.dissolveDensity);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGL] mask init failed", err);
            drawFallback(canvas, "Not supported (asset load failed)");
        });

        return state;
    }

    function initFlip(canvas, assets, variant, effectVariant, transitionSec, flipAngle) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const vs = [
            "precision mediump float;",
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "uniform float u_angle;",
            "uniform float u_axis;",
            "uniform vec2 u_pivot;",
            "uniform float u_depth;",
            "varying vec2 v_texCoord;",
            "varying float v_facing;",
            "void main() {",
            "  float c = cos(u_angle);",
            "  float s = sin(u_angle);",
            "  vec3 pos = vec3(a_position, 0.0);",
            "  pos.x -= u_pivot.x;",
            "  pos.y -= u_pivot.y;",
            "  vec3 rot;",
            "  if (u_axis < 0.5) {",
            "    rot = vec3(pos.x * c + pos.z * s, pos.y, -pos.x * s + pos.z * c);",
            "  } else {",
            "    rot = vec3(pos.x, pos.y * c - pos.z * s, pos.y * s + pos.z * c);",
            "  }",
            "  rot.x += u_pivot.x;",
            "  rot.y += u_pivot.y;",
            "  float z = rot.z + u_depth;",
            "  gl_Position = vec4(rot.x / z, rot.y / z, 0.0, 1.0);",
            "  v_texCoord = a_texCoord;",
            "  v_facing = step(0.0, c);",
            "}"
        ].join("\n");

        const fs = [
            "precision mediump float;",
            "uniform sampler2D u_tex1;",
            "uniform sampler2D u_tex2;",
            "uniform float u_angle;",
            "uniform float u_flipAxis;",
            "uniform vec2 u_effectOffset;",
            "uniform float u_effectScale;",
            "uniform float u_effectRotation;",
            "uniform float u_effectAlpha;",
            "varying vec2 v_texCoord;",
            "varying float v_facing;",
            "vec2 applyEffect(vec2 uv) {",
            "  vec2 centered = uv - 0.5;",
            "  float c = cos(u_effectRotation);",
            "  float s = sin(u_effectRotation);",
            "  vec2 rot = vec2(centered.x * c - centered.y * s, centered.x * s + centered.y * c);",
            "  return rot / u_effectScale + 0.5 + u_effectOffset;",
            "}",
            "void main() {",
            "  vec2 uv = v_texCoord;",
            "  if (v_facing < 0.5) {",
            "    if (u_flipAxis < 0.5) {",
            "      uv.x = 1.0 - uv.x;",
            "    } else {",
            "      uv.y = 1.0 - uv.y;",
            "    }",
            "  }",
            "  vec2 sampleUv = applyEffect(uv);",
            "  vec4 front = texture2D(u_tex1, clamp(sampleUv, 0.0, 1.0));",
            "  vec4 back = texture2D(u_tex2, clamp(sampleUv, 0.0, 1.0));",
            "  front.rgb *= u_effectAlpha;",
            "  back.rgb *= u_effectAlpha;",
            "  front.a *= u_effectAlpha;",
            "  back.a *= u_effectAlpha;",
            "  vec4 color = mix(back, front, v_facing);",
            "  float shade = 0.6 + 0.4 * abs(cos(u_angle));",
            "  gl_FragColor = vec4(color.rgb * shade, color.a);",
            "}"
        ].join("\n");

        const program = createProgram(gl, vs, fs);
        const bindQuad = createQuad(gl, program);
        const uAngle = gl.getUniformLocation(program, "u_angle");
        const uAxis = gl.getUniformLocation(program, "u_axis");
        const uPivot = gl.getUniformLocation(program, "u_pivot");
        const uDepth = gl.getUniformLocation(program, "u_depth");
        const uFlipAxis = gl.getUniformLocation(program, "u_flipAxis");
        const uTex1 = gl.getUniformLocation(program, "u_tex1");
        const uTex2 = gl.getUniformLocation(program, "u_tex2");
        const uEffectOffset = gl.getUniformLocation(program, "u_effectOffset");
        const uEffectScale = gl.getUniformLocation(program, "u_effectScale");
        const uEffectRotation = gl.getUniformLocation(program, "u_effectRotation");
        const uEffectAlpha = gl.getUniformLocation(program, "u_effectAlpha");

        const defaultTransitionSec = 2.6;
        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: 0,
            transitionSec: normalizeTransitionSeconds(transitionSec, defaultTransitionSec),
            flipAngle: resolveFlipAngle(flipAngle),
            assetA: null,
            assetB: null,
            currentImage: null,
            currentSwap: false,
            defaultTransitionSec,
            pause: function () {
                pauseState(state);
            },
            resume: function () {
                resumeState(state);
            },
            stop: function () {
                pauseState(state);
                state._frame = null;
                state._render = null;
            },
            resize: function () {
                resizeCanvasToDisplaySize(canvas, gl);
            }
        };

        const images = resolveImages("flip", assets);
        if (!images.flipA || !images.flipB) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }
        state.assetA = images.flipA;
        state.assetB = images.flipB;
        state.currentImage = images.flipA;
        state.currentSwap = false;
        const settings = getFlipSettings(variant);
        const effectSettings = effectVariant ? getEffectSettings(effectVariant) : null;
        Promise.all([loadImage(images.flipA), loadImage(images.flipB)]).then(([imgA, imgB]) => {
            const texA = createTexture(gl, imgA);
            const texB = createTexture(gl, imgB);

            gl.clearColor(0, 0, 0, 0);
            gl.useProgram(program);
            gl.uniform1i(uTex1, 0);
            gl.uniform1i(uTex2, 1);
            gl.uniform1f(uAxis, settings.axis);
            gl.uniform2f(uPivot, settings.pivotX, settings.pivotY);
            gl.uniform1f(uDepth, settings.depth);
            gl.uniform1f(uFlipAxis, settings.flipAxis);

            const start = performance.now();

            startLoop(state, (time) => {
                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(program);
                bindQuad();

                const elapsedSec = (time - start) * 0.001;
                const progress = computeTransitionProgress(elapsedSec, state.transitionSec, state.intervalSec);
                if (state.assetA && state.assetB) {
                    const isCurrentB = progress >= 0.5;
                    state.currentSwap = isCurrentB;
                    state.currentImage = isCurrentB ? state.assetB : state.assetA;
                }
                const minAngle = settings.minAngle || 0;
                const angleDegrees = Math.max(state.flipAngle, minAngle);
                const angle = progress * (angleDegrees * Math.PI / 180);
                const effectParams = effectSettings ? getEffectParams(effectSettings.type, elapsedSec) : null;
                const effectOffsetX = effectParams ? effectParams.offsetX : 0;
                const effectOffsetY = effectParams ? effectParams.offsetY : 0;
                const effectScale = effectParams ? effectParams.scale : 1;
                const effectRotation = effectParams ? effectParams.rotation : 0;
                const effectAlpha = effectParams ? effectParams.alpha : 1;

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, texA);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, texB);

                gl.uniform1f(uAngle, angle);
                gl.uniform2f(uEffectOffset, effectOffsetX, effectOffsetY);
                gl.uniform1f(uEffectScale, effectScale);
                gl.uniform1f(uEffectRotation, effectRotation);
                gl.uniform1f(uEffectAlpha, effectAlpha);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGL] flip init failed", err);
            drawFallback(canvas, "Not supported (asset load failed)");
        });

        return state;
    }

    function initTransition(canvas, assets, variant, effectVariant, transitionSec) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const vs = [
            "precision mediump float;",
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  v_texCoord = a_texCoord;",
            "  gl_Position = vec4(a_position, 0.0, 1.0);",
            "}"
        ].join("\n");

        const fs = [
            "precision mediump float;",
            "uniform sampler2D u_tex1;",
            "uniform sampler2D u_tex2;",
            "uniform float u_progress;",
            "uniform float u_variant;",
            "uniform vec2 u_direction;",
            "uniform vec2 u_effectOffset;",
            "uniform float u_effectScale;",
            "uniform float u_effectRotation;",
            "uniform float u_effectAlpha;",
            "varying vec2 v_texCoord;",
            "vec2 applyEffect(vec2 uv) {",
            "  vec2 centered = uv - 0.5;",
            "  float c = cos(u_effectRotation);",
            "  float s = sin(u_effectRotation);",
            "  vec2 rot = vec2(centered.x * c - centered.y * s, centered.x * s + centered.y * c);",
            "  return rot / u_effectScale + 0.5 + u_effectOffset;",
            "}",
            "float inside(vec2 uv) {",
            "  return step(0.0, uv.x) * step(0.0, uv.y) * step(uv.x, 1.0) * step(uv.y, 1.0);",
            "}",
            "vec4 sampleTexture(sampler2D tex, vec2 uv) {",
            "  vec2 transformed = applyEffect(uv);",
            "  vec4 color = texture2D(tex, clamp(transformed, 0.0, 1.0));",
            "  color.rgb *= u_effectAlpha;",
            "  color.a *= u_effectAlpha;",
            "  return color;",
            "}",
            "void main() {",
            "  vec2 uv = v_texCoord;",
            "  vec4 c1 = sampleTexture(u_tex1, uv);",
            "  vec4 c2 = sampleTexture(u_tex2, uv);",
            "  float p = clamp(u_progress, 0.0, 1.0);",
            "  vec2 dir = u_direction;",
            "  if (u_variant < 0.5) {",
            "    gl_FragColor = mix(c1, c2, p);",
            "    return;",
            "  }",
            "  if (u_variant < 1.5) {",
            "    float t = p * 2.0;",
            "    vec3 mid = vec3(0.0);",
            "    vec3 color = t < 1.0 ? mix(c1.rgb, mid, t) : mix(mid, c2.rgb, t - 1.0);",
            "    gl_FragColor = vec4(color, 1.0);",
            "    return;",
            "  }",
            "  if (u_variant < 2.5) {",
            "    float t = p * 2.0;",
            "    vec3 mid = vec3(1.0);",
            "    vec3 color = t < 1.0 ? mix(c1.rgb, mid, t) : mix(mid, c2.rgb, t - 1.0);",
            "    gl_FragColor = vec4(color, 1.0);",
            "    return;",
            "  }",
            "  if (u_variant < 3.5) {",
            "    vec2 uvOld = uv + dir * p;",
            "    vec2 uvNew = uv + dir * (p - 1.0);",
            "    float maskOld = inside(uvOld);",
            "    float maskNew = inside(uvNew);",
            "    vec4 old = sampleTexture(u_tex1, uvOld) * maskOld;",
            "    vec4 next = sampleTexture(u_tex2, uvNew) * maskNew;",
            "    gl_FragColor = mix(old, next, p);",
            "    return;",
            "  }",
            "  if (u_variant < 4.5) {",
            "    vec2 uvOld = uv + dir * p;",
            "    vec2 uvNew = uv + dir * (p - 1.0);",
            "    float maskOld = inside(uvOld);",
            "    float maskNew = inside(uvNew);",
            "    vec4 old = sampleTexture(u_tex1, uvOld) * maskOld;",
            "    vec4 next = sampleTexture(u_tex2, uvNew) * maskNew;",
            "    gl_FragColor = old + next;",
            "    return;",
            "  }",
            "  if (u_variant < 5.5) {",
            "    vec2 uvNew = uv + dir * (p - 1.0);",
            "    float maskNew = inside(uvNew);",
            "    vec4 next = sampleTexture(u_tex2, uvNew) * maskNew;",
            "    gl_FragColor = mix(c1, next, maskNew);",
            "    return;",
            "  }",
            "  if (u_variant < 6.5) {",
            "    vec2 uvOld = uv + dir * p;",
            "    float maskOld = inside(uvOld);",
            "    vec4 old = sampleTexture(u_tex1, uvOld) * maskOld;",
            "    gl_FragColor = mix(c2, old, maskOld);",
            "    return;",
            "  }",
            "  if (u_variant < 7.5) {",
            "    float edge = 0.0;",
            "    if (abs(dir.x) > 0.5) {",
            "      edge = dir.x < 0.0 ? 1.0 - uv.x : uv.x;",
            "    } else {",
            "      edge = dir.y < 0.0 ? 1.0 - uv.y : uv.y;",
            "    }",
            "    float edge0 = p - 0.02;",
            "    float edge1 = min(p + 0.02, 1.0);",
            "    float mask = smoothstep(edge0, edge1, edge);",
            "    gl_FragColor = mix(c1, c2, mask);",
            "    return;",
            "  }",
            "  if (u_variant < 8.5) {",
            "    float scaleNew = mix(0.85, 1.0, p);",
            "    vec2 uvNew = (uv - 0.5) / scaleNew + 0.5;",
            "    vec4 next = sampleTexture(u_tex2, uvNew);",
            "    gl_FragColor = mix(c1, next, p);",
            "    return;",
            "  }",
            "  if (u_variant < 9.5) {",
            "    float scaleNew = mix(1.15, 1.0, p);",
            "    vec2 uvNew = (uv - 0.5) / scaleNew + 0.5;",
            "    vec4 next = sampleTexture(u_tex2, uvNew);",
            "    gl_FragColor = mix(c1, next, p);",
            "    return;",
            "  }",
            "  float scaleOld = mix(1.0, 1.12, p);",
            "  float scaleNew = mix(0.88, 1.0, p);",
            "  vec2 uvOld = (uv - 0.5) / scaleOld + 0.5;",
            "  vec2 uvNew = (uv - 0.5) / scaleNew + 0.5;",
            "  vec4 old = sampleTexture(u_tex1, uvOld);",
            "  vec4 next = sampleTexture(u_tex2, uvNew);",
            "  gl_FragColor = mix(old, next, p);",
            "}"
        ].join("\n");

        const program = createProgram(gl, vs, fs);
        const bindQuad = createQuad(gl, program);
        const uProgress = gl.getUniformLocation(program, "u_progress");
        const uVariant = gl.getUniformLocation(program, "u_variant");
        const uDirection = gl.getUniformLocation(program, "u_direction");
        const uTex1 = gl.getUniformLocation(program, "u_tex1");
        const uTex2 = gl.getUniformLocation(program, "u_tex2");
        const uEffectOffset = gl.getUniformLocation(program, "u_effectOffset");
        const uEffectScale = gl.getUniformLocation(program, "u_effectScale");
        const uEffectRotation = gl.getUniformLocation(program, "u_effectRotation");
        const uEffectAlpha = gl.getUniformLocation(program, "u_effectAlpha");

        const defaultTransitionSec = 2.4;
        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: 0,
            transitionSec: normalizeTransitionSeconds(transitionSec, defaultTransitionSec),
            assetA: null,
            assetB: null,
            currentImage: null,
            currentSwap: false,
            defaultTransitionSec,
            pause: function () {
                pauseState(state);
            },
            resume: function () {
                resumeState(state);
            },
            stop: function () {
                pauseState(state);
                state._frame = null;
                state._render = null;
            },
            resize: function () {
                resizeCanvasToDisplaySize(canvas, gl);
            }
        };

        const images = resolveImages("transition", assets);
        if (!images.transitionA || !images.transitionB) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }
        state.assetA = images.transitionA;
        state.assetB = images.transitionB;
        state.currentImage = images.transitionA;
        state.currentSwap = false;
        const settings = getTransitionSettings(variant);
        const effectSettings = effectVariant ? getEffectSettings(effectVariant) : null;
        Promise.all([loadImage(images.transitionA), loadImage(images.transitionB)]).then(([imgA, imgB]) => {
            const texA = createTexture(gl, imgA);
            const texB = createTexture(gl, imgB);

            gl.clearColor(0, 0, 0, 0);
            gl.useProgram(program);
            gl.uniform1i(uTex1, 0);
            gl.uniform1i(uTex2, 1);
            gl.uniform1f(uVariant, settings.type);
            const dir = settings.dir || [0, 0];
            gl.uniform2f(uDirection, dir[0], dir[1]);

            const start = performance.now();

            startLoop(state, (time) => {
                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(program);
                bindQuad();

                const elapsedSec = (time - start) * 0.001;
                const progress = computeTransitionProgress(elapsedSec, state.transitionSec, state.intervalSec);
                if (state.assetA && state.assetB) {
                    const isCurrentB = progress >= 0.5;
                    state.currentSwap = isCurrentB;
                    state.currentImage = isCurrentB ? state.assetB : state.assetA;
                }
                const effectParams = effectSettings ? getEffectParams(effectSettings.type, elapsedSec) : null;
                const effectOffsetX = effectParams ? effectParams.offsetX : 0;
                const effectOffsetY = effectParams ? effectParams.offsetY : 0;
                const effectScale = effectParams ? effectParams.scale : 1;
                const effectRotation = effectParams ? effectParams.rotation : 0;
                const effectAlpha = effectParams ? effectParams.alpha : 1;

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, texA);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, texB);

                gl.uniform1f(uProgress, progress);
                gl.uniform2f(uEffectOffset, effectOffsetX, effectOffsetY);
                gl.uniform1f(uEffectScale, effectScale);
                gl.uniform1f(uEffectRotation, effectRotation);
                gl.uniform1f(uEffectAlpha, effectAlpha);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGL] transition init failed", err);
            drawFallback(canvas, "Not supported (asset load failed)");
        });

        return state;
    }

    function initEffect(canvas, assets, variant) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const vs = [
            "precision mediump float;",
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  v_texCoord = a_texCoord;",
            "  gl_Position = vec4(a_position, 0.0, 1.0);",
            "}"
        ].join("\n");

        const fs = [
            "precision mediump float;",
            "uniform sampler2D u_image;",
            "uniform vec2 u_offset;",
            "uniform float u_scale;",
            "uniform float u_rotation;",
            "uniform float u_alpha;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  vec2 uv = v_texCoord - 0.5;",
            "  float c = cos(u_rotation);",
            "  float s = sin(u_rotation);",
            "  vec2 rot = vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);",
            "  vec2 sampleUv = rot / u_scale + 0.5 + u_offset;",
            "  vec4 color = texture2D(u_image, sampleUv);",
            "  gl_FragColor = vec4(color.rgb, color.a * u_alpha);",
            "}"
        ].join("\n");

        const program = createProgram(gl, vs, fs);
        const bindQuad = createQuad(gl, program);
        const uImage = gl.getUniformLocation(program, "u_image");
        const uOffset = gl.getUniformLocation(program, "u_offset");
        const uScale = gl.getUniformLocation(program, "u_scale");
        const uRotation = gl.getUniformLocation(program, "u_rotation");
        const uAlpha = gl.getUniformLocation(program, "u_alpha");

        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: 0,
            pause: function () {
                pauseState(state);
            },
            resume: function () {
                resumeState(state);
            },
            stop: function () {
                pauseState(state);
                state._frame = null;
                state._render = null;
            },
            resize: function () {
                resizeCanvasToDisplaySize(canvas, gl);
            }
        };

        const images = resolveImages("effect", assets);
        if (!images.effect) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }
        const settings = getEffectSettings(variant);
        loadImage(images.effect).then((img) => {
            const imageTexture = createTexture(gl, img);

            gl.clearColor(0, 0, 0, 0);
            gl.useProgram(program);
            gl.uniform1i(uImage, 0);

            const start = performance.now();

            startLoop(state, (time) => {
                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(program);
                bindQuad();

                const elapsed = (time - start) * 0.001;
                const params = getEffectParams(settings.type, elapsed);

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, imageTexture);

                gl.uniform2f(uOffset, params.offsetX, params.offsetY);
                gl.uniform1f(uScale, params.scale);
                gl.uniform1f(uRotation, params.rotation);
                gl.uniform1f(uAlpha, params.alpha);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGL] effect init failed", err);
            drawFallback(canvas, "Not supported (asset load failed)");
        });

        return state;
    }

    function initTextFill(canvas, assets, variant) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const vs = [
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  v_texCoord = a_texCoord;",
            "  gl_Position = vec4(a_position, 0.0, 1.0);",
            "}"
        ].join("\n");

        const fs = [
            "precision mediump float;",
            "uniform sampler2D u_image;",
            "uniform sampler2D u_mask;",
            "uniform vec2 u_offset;",
            "uniform float u_scale;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  vec2 uv = v_texCoord;",
            "  vec2 imgUv = (uv - 0.5) / u_scale + 0.5 + u_offset;",
            "  vec4 img = texture2D(u_image, imgUv);",
            "  float mask = texture2D(u_mask, uv).a;",
            "  gl_FragColor = vec4(img.rgb, img.a * mask);",
            "}"
        ].join("\n");

        const program = createProgram(gl, vs, fs);
        const bindQuad = createQuad(gl, program);
        const uImage = gl.getUniformLocation(program, "u_image");
        const uMask = gl.getUniformLocation(program, "u_mask");
        const uOffset = gl.getUniformLocation(program, "u_offset");
        const uScale = gl.getUniformLocation(program, "u_scale");

        const maskCanvas = document.createElement("canvas");
        const maskCtx = maskCanvas.getContext("2d");
        const maskTexture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, maskTexture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: 0,
            pause: function () {
                pauseState(state);
            },
            resume: function () {
                resumeState(state);
            },
            stop: function () {
                pauseState(state);
                state._frame = null;
                state._render = null;
            },
            resize: function () {
                if (resizeCanvasToDisplaySize(canvas, gl)) {
                    updateMask();
                }
            }
        };

        function updateMask() {
            if (!maskCtx) {
                return;
            }
            const width = canvas.width;
            const height = canvas.height;
            if (width <= 0 || height <= 0) {
                return;
            }
            maskCanvas.width = width;
            maskCanvas.height = height;
            maskCtx.clearRect(0, 0, width, height);
            const fontSize = Math.max(32, Math.floor(height * 0.45));
            maskCtx.fillStyle = "#ffffff";
            maskCtx.textAlign = "center";
            maskCtx.textBaseline = "middle";
            maskCtx.font = "bold " + fontSize + "px sans-serif";
            maskCtx.fillText("FLOWERY", width / 2, height / 2);

            gl.bindTexture(gl.TEXTURE_2D, maskTexture);
            gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, maskCanvas);
        }

        const images = resolveImages("text", assets);
        if (!images.text) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }
        loadImage(images.text).then((img) => {
            const imageTexture = createTexture(gl, img);

            gl.clearColor(0, 0, 0, 0);
            gl.useProgram(program);
            gl.uniform1i(uImage, 0);
            gl.uniform1i(uMask, 1);

            const start = performance.now();
            const scale = 1.2;
            updateMask();

            startLoop(state, (time) => {
                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(program);
                bindQuad();

                const elapsed = (time - start) * 0.001;
                const offsetX = Math.sin(elapsed * 0.6) * 0.08;
                const offsetY = Math.cos(elapsed * 0.4) * 0.06;

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, imageTexture);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, maskTexture);

                gl.uniform2f(uOffset, offsetX, offsetY);
                gl.uniform1f(uScale, scale);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGL] text init failed", err);
            drawFallback(canvas, "Not supported (asset load failed)");
        });

        return state;
    }

    function stop(canvas) {
        if (!canvas || !canvas._floweryGl) {
            return;
        }
        const state = canvas._floweryGl;
        if (state.stop) {
            state.stop();
        }
        canvas._floweryGl = null;
    }

    function resize(canvas) {
        if (!canvas || !canvas._floweryGl) {
            return;
        }
        const state = canvas._floweryGl;
        if (state.resize) {
            state.resize();
        }
    }

    function init(
        canvas,
        mode,
        assets,
        variant,
        effectVariant,
        transitionSec,
        sliceCount,
        stagger,
        staggerMs,
        pixelateSize,
        dissolveDensity,
        flipAngle) {
        if (!canvas) {
            return;
        }

        stop(canvas);

        let state = null;
        try {
            if (mode === "mask") {
                state = initMask(
                    canvas,
                    assets,
                    variant,
                    effectVariant,
                    transitionSec,
                    sliceCount,
                    stagger,
                    staggerMs,
                    pixelateSize,
                    dissolveDensity);
            } else if (mode === "flip") {
                state = initFlip(canvas, assets, variant, effectVariant, transitionSec, flipAngle);
            } else if (mode === "transition") {
                state = initTransition(canvas, assets, variant, effectVariant, transitionSec);
            } else if (mode === "effect") {
                state = initEffect(canvas, assets, variant);
            } else if (mode === "text") {
                state = initTextFill(canvas, assets, variant);
            } else {
                console.error("[CarouselGL] unknown mode", mode);
            }
        } catch (err) {
            console.error("[CarouselGL] init failed", err);
        }

        if (state) {
            canvas._floweryGl = state;
        }
    }

    function ensureDebugHost() {
        let host = document.getElementById("flowery-gl-debug");
        if (host) {
            return host;
        }

        host = document.createElement("div");
        host.id = "flowery-gl-debug";
        host.style.position = "fixed";
        host.style.right = "16px";
        host.style.bottom = "16px";
        host.style.width = "360px";
        host.style.padding = "10px";
        host.style.borderRadius = "12px";
        host.style.background = "rgba(0,0,0,0.65)";
        host.style.border = "1px solid rgba(255,255,255,0.15)";
        host.style.display = "grid";
        host.style.gap = "10px";
        host.style.zIndex = "2147483647";
        host.style.color = "#fff";
        host.style.font = "12px/1.4 system-ui, sans-serif";
        host.style.pointerEvents = "none";
        return host;
    }

    function createDebugCanvas(host, labelText) {
        const wrapper = document.createElement("div");
        wrapper.style.display = "grid";
        wrapper.style.gap = "6px";

        const label = document.createElement("div");
        label.textContent = labelText;
        label.style.opacity = "0.8";

        const canvas = document.createElement("canvas");
        canvas.style.width = "100%";
        canvas.style.height = "140px";
        canvas.style.display = "block";
        canvas.style.background = "rgba(0,0,0,0.2)";
        canvas.style.borderRadius = "8px";
        canvas.width = 320;
        canvas.height = 140;

        wrapper.appendChild(label);
        wrapper.appendChild(canvas);
        host.appendChild(wrapper);
        return canvas;
    }

    function debugStart() {
        const existing = document.getElementById("flowery-gl-debug");
        if (existing) {
            return;
        }

        const host = ensureDebugHost();
        const canvases = [];
        const entries = [
            { mode: "mask", label: "CarouselGL Mask" },
            { mode: "flip", label: "CarouselGL Flip" },
            { mode: "text", label: "CarouselGL Text Fill" }
        ];

        entries.forEach((entry) => {
            const canvas = createDebugCanvas(host, entry.label);
            canvases.push(canvas);
            init(canvas, entry.mode, null, null, null, 0, 0, false, 0, 20, 0.5, 180);
        });

        host._floweryCanvases = canvases;
        document.body.appendChild(host);
    }

    function debugStop() {
        const host = document.getElementById("flowery-gl-debug");
        if (!host) {
            return;
        }

        const canvases = host._floweryCanvases || [];
        canvases.forEach((canvas) => stop(canvas));
        host.remove();
    }

    const overlayMap = new Map();

    function ensureOverlayCanvas(key) {
        let entry = overlayMap.get(key);
        if (entry) {
            return entry;
        }

        const canvas = document.createElement("canvas");
        canvas.style.position = "fixed";
        canvas.style.left = "0px";
        canvas.style.top = "0px";
        canvas.style.width = "1px";
        canvas.style.height = "1px";
        canvas.style.display = "block";
        canvas.style.pointerEvents = "none";
        canvas.style.zIndex = String(getOverlayZIndex());
        canvas.style.borderRadius = "12px";
        canvas.style.background = "transparent";
        canvas.width = 1;
        canvas.height = 1;

        document.body.appendChild(canvas);
        entry = {
            canvas,
            mode: null,
            assetsKey: null,
            variant: null,
            effectVariant: null,
            intervalSec: 0,
            transitionSec: 0,
            sliceCount: 0,
            stagger: true,
            staggerMs: 50,
            pixelateSize: 20,
            dissolveDensity: 0.5,
            flipAngle: 180
        };
        overlayMap.set(key, entry);
        return entry;
    }

    function updateOverlayRect(canvas, left, top, width, height) {
        if (!Number.isFinite(left) || !Number.isFinite(top) || !Number.isFinite(width) || !Number.isFinite(height)) {
            return;
        }

        const clampedWidth = Math.max(1, width);
        const clampedHeight = Math.max(1, height);
        canvas.style.left = left + "px";
        canvas.style.top = top + "px";
        canvas.style.width = clampedWidth + "px";
        canvas.style.height = clampedHeight + "px";
        resize(canvas);
    }

    function attachOverlay(
        key,
        mode,
        left,
        top,
        width,
        height,
        assets,
        variant,
        effectVariant,
        transitionSec,
        sliceCount,
        stagger,
        staggerMs,
        pixelateSize,
        dissolveDensity,
        flipAngle) {
        if (!key || !mode) {
            return;
        }

        const entry = ensureOverlayCanvas(key);
        const assetsKey = assets ? JSON.stringify(assets) : "";
        const variantKey = variant || "";
        const effectKey = effectVariant || "";
        const transitionValue = Number(transitionSec);
        const nextTransitionSec = Number.isFinite(transitionValue) ? transitionValue : 0;
        const nextSliceCount = resolveSliceCount(sliceCount);
        const nextStagger = resolveStaggerFlag(stagger);
        const nextStaggerMs = resolveStaggerMs(staggerMs);
        const pixelValue = resolvePixelateSize(pixelateSize);
        const nextDissolveDensity = resolveDissolveDensity(dissolveDensity);
        const nextFlipAngle = resolveFlipAngle(flipAngle);
        const previousState = entry.canvas._floweryGl;
        const resolvedAssets = resolveAssetsForCurrentImage(mode, assets, previousState);
        if (entry.mode !== mode || entry.assetsKey !== assetsKey || entry.variant !== variantKey || entry.effectVariant !== effectKey) {
            entry.mode = mode;
            entry.assetsKey = assetsKey;
            entry.variant = variantKey;
            entry.effectVariant = effectKey;
            entry.transitionSec = nextTransitionSec;
            entry.sliceCount = nextSliceCount;
            entry.stagger = nextStagger;
            entry.staggerMs = nextStaggerMs;
            entry.pixelateSize = pixelValue;
            entry.dissolveDensity = nextDissolveDensity;
            entry.flipAngle = nextFlipAngle;
            init(
                entry.canvas,
                mode,
                resolvedAssets || null,
                variant || null,
                effectVariant || null,
                nextTransitionSec,
                nextSliceCount,
                nextStagger,
                nextStaggerMs,
                pixelValue,
                nextDissolveDensity,
                nextFlipAngle);
        } else if (entry.transitionSec !== nextTransitionSec
            || entry.sliceCount !== nextSliceCount
            || entry.stagger !== nextStagger
            || entry.staggerMs !== nextStaggerMs
            || entry.pixelateSize !== pixelValue
            || entry.dissolveDensity !== nextDissolveDensity
            || entry.flipAngle !== nextFlipAngle) {
            entry.transitionSec = nextTransitionSec;
            entry.sliceCount = nextSliceCount;
            entry.stagger = nextStagger;
            entry.staggerMs = nextStaggerMs;
            entry.pixelateSize = pixelValue;
            entry.dissolveDensity = nextDissolveDensity;
            entry.flipAngle = nextFlipAngle;
            setOverlayTransitionDuration(key, nextTransitionSec);
            setOverlayTransitionParams(key, nextSliceCount, nextStagger, nextStaggerMs, pixelValue, nextDissolveDensity, nextFlipAngle);
        }
        updateOverlayRect(entry.canvas, left, top, width, height);
    }

    function detachOverlay(key) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        stop(entry.canvas);
        entry.canvas.remove();
        overlayMap.delete(key);
    }

    function detachAllOverlays() {
        Array.from(overlayMap.keys()).forEach((key) => detachOverlay(key));
    }

    function setOverlayTiming(key, intervalSec) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        const seconds = Number(intervalSec);
        entry.intervalSec = Number.isFinite(seconds) && seconds > 0 ? seconds : 0;
        if (entry.canvas && entry.canvas._floweryGl) {
            entry.canvas._floweryGl.intervalSec = entry.intervalSec;
        }
    }

    function setOverlayTransitionDuration(key, transitionSec) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        const seconds = Number(transitionSec);
        entry.transitionSec = Number.isFinite(seconds) ? seconds : 0;
        if (entry.canvas && entry.canvas._floweryGl) {
            const state = entry.canvas._floweryGl;
            const fallback = Number.isFinite(state.defaultTransitionSec) ? state.defaultTransitionSec : 0;
            state.baseTransitionSec = entry.transitionSec;
            state.transitionSec = normalizeTransitionSeconds(entry.transitionSec, fallback);
        }
    }

    function setOverlayTransitionParams(
        key,
        sliceCount,
        stagger,
        staggerMs,
        pixelateSize,
        dissolveDensity,
        flipAngle) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        const nextSliceCount = resolveSliceCount(sliceCount);
        const nextStagger = resolveStaggerFlag(stagger);
        const nextStaggerMs = resolveStaggerMs(staggerMs);
        const nextPixelateSize = resolvePixelateSize(pixelateSize);
        const nextDissolveDensity = resolveDissolveDensity(dissolveDensity);
        const nextFlipAngle = resolveFlipAngle(flipAngle);

        entry.sliceCount = nextSliceCount;
        entry.stagger = nextStagger;
        entry.staggerMs = nextStaggerMs;
        entry.pixelateSize = nextPixelateSize;
        entry.dissolveDensity = nextDissolveDensity;
        entry.flipAngle = nextFlipAngle;

        if (entry.canvas && entry.canvas._floweryGl) {
            const state = entry.canvas._floweryGl;
            state.sliceCount = nextSliceCount;
            state.stagger = nextStagger;
            state.staggerMs = nextStaggerMs;
            state.pixelateSize = nextPixelateSize;
            state.dissolveDensity = nextDissolveDensity;
            state.flipAngle = nextFlipAngle;
        }
    }

    function setOverlayActive(key, isActive) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        if (isActive) {
            entry.canvas.style.display = "block";
            if (entry.canvas._floweryGl && entry.canvas._floweryGl.resume) {
                entry.canvas._floweryGl.resume();
            }
            resize(entry.canvas);
        } else {
            if (entry.canvas._floweryGl && entry.canvas._floweryGl.pause) {
                entry.canvas._floweryGl.pause();
            }
            entry.canvas.style.display = "none";
        }
    }

    window.FloweryCarouselGL = {
        init,
        stop,
        resize,
        debugStart,
        debugStop,
        attachOverlay,
        detachOverlay,
        detachAllOverlays,
        setOverlayTiming,
        setOverlayTransitionDuration,
        setOverlayTransitionParams,
        setOverlayActive
    };
})();
