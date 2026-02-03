(function () {
    "use strict";

    const DOCUMENT_BASE = new URL(".", document.baseURI).toString();
    const FRAMEWORK_BASE = new URL("_framework/", DOCUMENT_BASE).toString();
    const DEFAULT_OVERLAY_Z_INDEX = 1000;
    const DEFAULT_TRANSITION_SEC = 0.6;
    const TRANSITION_DURATION_SCALE = 1.25;
    const CIRCLE_TRANSITION_SCALE = 2.0;

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

    function getTransitionDurationScale(transitionName) {
        if (!transitionName) {
            return TRANSITION_DURATION_SCALE;
        }

        const key = String(transitionName).trim().toLowerCase();
        if (key === "circle") {
            return TRANSITION_DURATION_SCALE * CIRCLE_TRANSITION_SCALE;
        }

        return TRANSITION_DURATION_SCALE;
    }

    function normalizeTransitionSeconds(value, fallback, transitionName) {
        const seconds = Number(value);
        const scale = getTransitionDurationScale(transitionName);
        if (Number.isFinite(seconds) && seconds > 0) {
            return seconds * scale;
        }
        return fallback * scale;
    }

    function resolveAssetPath(path) {
        if (!path) {
            return path;
        }
        if (/^(https?:)?\/\//.test(path) || path.startsWith("data:")) {
            return path;
        }
        const cleaned = path.startsWith("/") ? path.slice(1) : path;
        return new URL(cleaned, FRAMEWORK_BASE).toString();
    }

    const DEFAULT_IMAGES = [
        "Assets/wallpaper_blue.jpg",
        "Assets/wallpaper_green.jpg",
        "Assets/wallpaper_red.jpg",
        "Assets/wallpaper_yellow.jpg"
    ];

    const GL_TRANSITIONS = [
        { name: "angular", path: "WasmScripts/gl-transitions/angular.glsl" },
        { name: "BookFlip", path: "WasmScripts/gl-transitions/BookFlip.glsl" },
        { name: "Bounce", path: "WasmScripts/gl-transitions/Bounce.glsl" },
        { name: "BowTieHorizontal", path: "WasmScripts/gl-transitions/BowTieHorizontal.glsl" },
        { name: "BowTieVertical", path: "WasmScripts/gl-transitions/BowTieVertical.glsl" },
        { name: "BowTieWithParameter", path: "WasmScripts/gl-transitions/BowTieWithParameter.glsl" },
        { name: "burn", path: "WasmScripts/gl-transitions/burn.glsl" },
        { name: "ButterflyWaveScrawler", path: "WasmScripts/gl-transitions/ButterflyWaveScrawler.glsl" },
        { name: "cannabisleaf", path: "WasmScripts/gl-transitions/cannabisleaf.glsl" },
        { name: "circle", path: "WasmScripts/gl-transitions/circle.glsl?v=2" },
        { name: "CircleCrop", path: "WasmScripts/gl-transitions/CircleCrop.glsl?v=2" },
        { name: "circleopen", path: "WasmScripts/gl-transitions/circleopen.glsl" },
        { name: "colorphase", path: "WasmScripts/gl-transitions/colorphase.glsl" },
        { name: "ColourDistance", path: "WasmScripts/gl-transitions/ColourDistance.glsl" },
        { name: "coord-from-in", path: "WasmScripts/gl-transitions/coord-from-in.glsl" },
        { name: "CrazyParametricFun", path: "WasmScripts/gl-transitions/CrazyParametricFun.glsl" },
        { name: "crosshatch", path: "WasmScripts/gl-transitions/crosshatch.glsl" },
        { name: "crosswarp", path: "WasmScripts/gl-transitions/crosswarp.glsl" },
        { name: "CrossZoom", path: "WasmScripts/gl-transitions/CrossZoom.glsl" },
        { name: "cube", path: "WasmScripts/gl-transitions/cube.glsl" },
        { name: "Directional", path: "WasmScripts/gl-transitions/Directional.glsl" },
        { name: "directional-easing", path: "WasmScripts/gl-transitions/directional-easing.glsl" },
        { name: "DirectionalScaled", path: "WasmScripts/gl-transitions/DirectionalScaled.glsl" },
        { name: "directionalwarp", path: "WasmScripts/gl-transitions/directionalwarp.glsl" },
        { name: "directionalwipe", path: "WasmScripts/gl-transitions/directionalwipe.glsl" },
        { name: "displacement", path: "WasmScripts/gl-transitions/displacement.glsl" },
        { name: "dissolve", path: "WasmScripts/gl-transitions/dissolve.glsl/dissolve.glsl" },
        { name: "DoomScreenTransition", path: "WasmScripts/gl-transitions/DoomScreenTransition.glsl" },
        { name: "doorway", path: "WasmScripts/gl-transitions/doorway.glsl" },
        { name: "Dreamy", path: "WasmScripts/gl-transitions/Dreamy.glsl" },
        { name: "DreamyZoom", path: "WasmScripts/gl-transitions/DreamyZoom.glsl" },
        { name: "EdgeTransition", path: "WasmScripts/gl-transitions/EdgeTransition.glsl" },
        { name: "fade", path: "WasmScripts/gl-transitions/fade.glsl" },
        { name: "fadecolor", path: "WasmScripts/gl-transitions/fadecolor.glsl?v=3" },
        { name: "fadegrayscale", path: "WasmScripts/gl-transitions/fadegrayscale.glsl" },
        { name: "FilmBurn", path: "WasmScripts/gl-transitions/FilmBurn.glsl" },
        { name: "flyeye", path: "WasmScripts/gl-transitions/flyeye.glsl" },
        { name: "GlitchDisplace", path: "WasmScripts/gl-transitions/GlitchDisplace.glsl" },
        { name: "GlitchMemories", path: "WasmScripts/gl-transitions/GlitchMemories.glsl" },
        { name: "GridFlip", path: "WasmScripts/gl-transitions/GridFlip.glsl" },
        { name: "heart", path: "WasmScripts/gl-transitions/heart.glsl" },
        { name: "hexagonalize", path: "WasmScripts/gl-transitions/hexagonalize.glsl" },
        { name: "HorizontalClose", path: "WasmScripts/gl-transitions/HorizontalClose.glsl" },
        { name: "HorizontalOpen", path: "WasmScripts/gl-transitions/HorizontalOpen.glsl" },
        { name: "InvertedPageCurl", path: "WasmScripts/gl-transitions/InvertedPageCurl.glsl?v=2" },
        { name: "kaleidoscope", path: "WasmScripts/gl-transitions/kaleidoscope.glsl" },
        { name: "LeftRight", path: "WasmScripts/gl-transitions/LeftRight.glsl" },
        { name: "LinearBlur", path: "WasmScripts/gl-transitions/LinearBlur.glsl" },
        { name: "luma", path: "WasmScripts/gl-transitions/luma.glsl" },
        { name: "luminance_melt", path: "WasmScripts/gl-transitions/luminance_melt.glsl" },
        { name: "morph", path: "WasmScripts/gl-transitions/morph.glsl" },
        { name: "Mosaic", path: "WasmScripts/gl-transitions/Mosaic.glsl" },
        { name: "mosaic_transition", path: "WasmScripts/gl-transitions/mosaic_transition.glsl" },
        { name: "multiply_blend", path: "WasmScripts/gl-transitions/multiply_blend.glsl" },
        { name: "Overexposure", path: "WasmScripts/gl-transitions/Overexposure.glsl" },
        { name: "perlin", path: "WasmScripts/gl-transitions/perlin.glsl" },
        { name: "pinwheel", path: "WasmScripts/gl-transitions/pinwheel.glsl" },
        { name: "pixelize", path: "WasmScripts/gl-transitions/pixelize.glsl" },
        { name: "polar_function", path: "WasmScripts/gl-transitions/polar_function.glsl" },
        { name: "PolkaDotsCurtain", path: "WasmScripts/gl-transitions/PolkaDotsCurtain.glsl" },
        { name: "powerKaleido", path: "WasmScripts/gl-transitions/powerKaleido.glsl" },
        { name: "Radial", path: "WasmScripts/gl-transitions/Radial.glsl" },
        { name: "randomNoisex", path: "WasmScripts/gl-transitions/randomNoisex.glsl" },
        { name: "randomsquares", path: "WasmScripts/gl-transitions/randomsquares.glsl" },
        { name: "Rectangle", path: "WasmScripts/gl-transitions/Rectangle.glsl" },
        { name: "RectangleCrop", path: "WasmScripts/gl-transitions/RectangleCrop.glsl" },
        { name: "ripple", path: "WasmScripts/gl-transitions/ripple.glsl" },
        { name: "Rolls", path: "WasmScripts/gl-transitions/Rolls.glsl" },
        { name: "rotate_scale_fade", path: "WasmScripts/gl-transitions/rotate_scale_fade.glsl" },
        { name: "RotateScaleVanish", path: "WasmScripts/gl-transitions/RotateScaleVanish.glsl" },
        { name: "rotateTransition", path: "WasmScripts/gl-transitions/rotateTransition.glsl" },
        { name: "scale-in", path: "WasmScripts/gl-transitions/scale-in.glsl" },
        { name: "SimpleZoom", path: "WasmScripts/gl-transitions/SimpleZoom.glsl" },
        { name: "SimpleZoomOut", path: "WasmScripts/gl-transitions/SimpleZoomOut" },
        { name: "Slides", path: "WasmScripts/gl-transitions/Slides.glsl" },
        { name: "squareswire", path: "WasmScripts/gl-transitions/squareswire.glsl" },
        { name: "squeeze", path: "WasmScripts/gl-transitions/squeeze.glsl" },
        { name: "static_wipe", path: "WasmScripts/gl-transitions/static_wipe.glsl" },
        { name: "StaticFade", path: "WasmScripts/gl-transitions/StaticFade.glsl" },
        { name: "StereoViewer", path: "WasmScripts/gl-transitions/StereoViewer.glsl" },
        { name: "swap", path: "WasmScripts/gl-transitions/swap.glsl" },
        { name: "Swirl", path: "WasmScripts/gl-transitions/Swirl.glsl" },
        { name: "tangentMotionBlur", path: "WasmScripts/gl-transitions/tangentMotionBlur.glsl" },
        { name: "TopBottom", path: "WasmScripts/gl-transitions/TopBottom.glsl" },
        { name: "TVStatic", path: "WasmScripts/gl-transitions/TVStatic.glsl" },
        { name: "undulatingBurnOut", path: "WasmScripts/gl-transitions/undulatingBurnOut.glsl?v=2" },
        { name: "VerticalClose", path: "WasmScripts/gl-transitions/VerticalClose.glsl" },
        { name: "VerticalOpen", path: "WasmScripts/gl-transitions/VerticalOpen.glsl" },
        { name: "WaterDrop", path: "WasmScripts/gl-transitions/WaterDrop.glsl" },
        { name: "wind", path: "WasmScripts/gl-transitions/wind.glsl" },
        { name: "windowblinds", path: "WasmScripts/gl-transitions/windowblinds.glsl" },
        { name: "windowslice", path: "WasmScripts/gl-transitions/windowslice.glsl" },
        { name: "wipeDown", path: "WasmScripts/gl-transitions/wipeDown.glsl" },
        { name: "wipeLeft", path: "WasmScripts/gl-transitions/wipeLeft.glsl" },
        { name: "wipeRight", path: "WasmScripts/gl-transitions/wipeRight.glsl" },
        { name: "wipeUp", path: "WasmScripts/gl-transitions/wipeUp.glsl" },
        { name: "x_axis_translation", path: "WasmScripts/gl-transitions/x_axis_translation.glsl" },
        { name: "ZoomInCircles", path: "WasmScripts/gl-transitions/ZoomInCircles.glsl" },
        { name: "ZoomLeftWipe", path: "WasmScripts/gl-transitions/ZoomLeftWipe.glsl" },
        { name: "ZoomRigthWipe", path: "WasmScripts/gl-transitions/ZoomRigthWipe.glsl" }
    ];

    const TRANSITION_MAP = new Map();
    GL_TRANSITIONS.forEach((entry) => {
        TRANSITION_MAP.set(entry.name.toLowerCase(), entry);
    });

    const shaderCache = new Map();
    const imageCache = new Map();

    function resolveTransitionEntry(name) {
        if (!name) {
            return GL_TRANSITIONS.length > 0 ? GL_TRANSITIONS[0] : null;
        }

        const key = String(name).trim();
        if (!key) {
            return GL_TRANSITIONS.length > 0 ? GL_TRANSITIONS[0] : null;
        }

        if (key.toLowerCase() === "random") {
            return GL_TRANSITIONS.length > 0
                ? GL_TRANSITIONS[Math.floor(Math.random() * GL_TRANSITIONS.length)]
                : null;
        }

        return TRANSITION_MAP.get(key.toLowerCase()) || GL_TRANSITIONS[0] || null;
    }

    function resolveImages(assets) {
        const merged = Object.assign({}, assets || {});
        const list = [];

        if (Array.isArray(merged.images)) {
            merged.images.forEach((entry) => {
                if (entry) {
                    list.push(entry);
                }
            });
        }

        const from = merged.from || merged.transitionA;
        const to = merged.to || merged.transitionB;
        if (from) {
            list.push(from);
        }
        if (to && to !== from) {
            list.push(to);
        }

        if (list.length < 2) {
            return { list: DEFAULT_IMAGES.slice() };
        }

        return { list };
    }

    function loadImage(src) {
        const resolved = resolveAssetPath(src);
        if (imageCache.has(resolved)) {
            return imageCache.get(resolved);
        }

        const promise = new Promise((resolve, reject) => {
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = () => resolve(img);
            img.onerror = () => {
                reject(new Error("Failed to load image: " + resolved));
            };
            img.src = resolved;
        });

        imageCache.set(resolved, promise);
        return promise;
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

    function createSolidTexture(gl, r, g, b, a) {
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        const data = new Uint8Array([r, g, b, a]);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, data);
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

    function computeTransitionProgress(elapsedSec, transitionSec, intervalSec) {
        const holdSec = intervalSec > 0 ? intervalSec : 0;
        const transition = Math.max(0.001, transitionSec);
        const cycle = holdSec + transition + holdSec;
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
        return 1;
    }

    function getTransitionCycleSeconds(transitionSec, intervalSec) {
        const holdSec = intervalSec > 0 ? intervalSec : 0;
        const transition = Math.max(0.001, transitionSec);
        return holdSec + transition + holdSec;
    }
    function extractDefaultExpression(line) {
        let comment = null;
        const inlineIndex = line.indexOf("//");
        const blockIndex = line.indexOf("/*");

        if (inlineIndex >= 0) {
            comment = line.slice(inlineIndex + 2);
        } else if (blockIndex >= 0) {
            const end = line.indexOf("*/", blockIndex + 2);
            comment = line.slice(blockIndex + 2, end >= 0 ? end : undefined);
        }

        if (!comment) {
            return null;
        }

        const eqIndex = comment.indexOf("=");
        if (eqIndex < 0) {
            return null;
        }

        let expr = comment.slice(eqIndex + 1).trim();
        const extraIndex = expr.indexOf("//");
        if (extraIndex >= 0) {
            expr = expr.slice(0, extraIndex).trim();
        }
        if (expr.endsWith(";")) {
            expr = expr.slice(0, -1).trim();
        }
        return expr || null;
    }

    function extractNumbers(text) {
        const matches = text.match(/-?\d*\.?\d+(?:e[-+]?\d+)?/gi);
        if (!matches) {
            return [];
        }
        const numbers = [];
        for (let i = 0; i < matches.length; i++) {
            const value = Number(matches[i]);
            if (Number.isFinite(value)) {
                numbers.push(value);
            }
        }
        return numbers;
    }

    function extractConstructorArgs(text) {
        const trimmed = text.trim();
        const match = trimmed.match(/^[a-zA-Z_]\w*\s*\((.*)\)$/);
        return match ? match[1] : trimmed;
    }

    function parseDefaultValue(type, expr) {
        if (!expr) {
            return null;
        }

        const cleaned = expr.trim();
        if (!cleaned) {
            return null;
        }

        if (type === "float") {
            const numbers = extractNumbers(cleaned);
            return numbers.length > 0 ? numbers[0] : null;
        }

        if (type === "int") {
            const numbers = extractNumbers(cleaned);
            return numbers.length > 0 ? Math.trunc(numbers[0]) : null;
        }

        if (type === "bool") {
            if (/true/i.test(cleaned)) {
                return true;
            }
            if (/false/i.test(cleaned)) {
                return false;
            }
            const numbers = extractNumbers(cleaned);
            if (numbers.length > 0) {
                return numbers[0] !== 0;
            }
            return null;
        }

        if (type === "vec2" || type === "vec3" || type === "vec4" ||
            type === "ivec2" || type === "ivec3" || type === "ivec4") {
            const args = extractConstructorArgs(cleaned);
            const numbers = extractNumbers(args);
            if (numbers.length === 0) {
                return null;
            }
            const expected = Number(type.slice(-1));
            const values = [];
            if (numbers.length === 1) {
                for (let i = 0; i < expected; i++) {
                    values.push(numbers[0]);
                }
            } else {
                for (let i = 0; i < expected; i++) {
                    values.push(numbers[i] !== undefined ? numbers[i] : numbers[numbers.length - 1]);
                }
            }
            return values;
        }

        return null;
    }

    function parseUniforms(source) {
        const uniforms = [];
        const samplers = [];
        const lines = source.split(/\r?\n/);

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            if (!line.startsWith("uniform ")) {
                continue;
            }

            const match = line.match(/^uniform\s+(\w+)\s+([^;]+);/);
            if (!match) {
                continue;
            }

            const type = match[1];
            let namesPart = match[2];
            namesPart = namesPart.replace(/\/\*.*?\*\//g, "");
            namesPart = namesPart.split("//")[0];

            const names = namesPart.split(",").map((entry) => {
                const trimmed = entry.trim();
                if (!trimmed) {
                    return null;
                }
                const name = trimmed.split(/\s+/)[0].replace(/\[.*\]$/, "");
                return name || null;
            }).filter(Boolean);

            if (names.length === 0) {
                continue;
            }

            if (type === "sampler2D") {
                names.forEach((name) => samplers.push(name));
                continue;
            }

            const expr = extractDefaultExpression(line);
            if (!expr) {
                continue;
            }

            const value = parseDefaultValue(type, expr);
            if (value === null) {
                continue;
            }

            names.forEach((name) => {
                if (name === "progress" || name === "ratio") {
                    return;
                }
                uniforms.push({ name, type, value });
            });
        }

        return { uniforms, samplers };
    }

    function buildFragmentShader(source) {
        return [
            "precision mediump float;",
            "uniform sampler2D from;",
            "uniform sampler2D to;",
            "uniform float progress;",
            "uniform float ratio;",
            "varying vec2 v_texCoord;",
            "float floweryProgress;",
            "vec4 getFromColor(vec2 uv) {",
            "  return texture2D(from, uv);",
            "}",
            "vec4 getToColor(vec2 uv) {",
            "  return texture2D(to, uv);",
            "}",
            "#define progress floweryProgress",
            source,
            "#undef progress",
            "void main() {",
            "  vec2 uv = v_texCoord;",
            "  float clampedProgress = clamp(progress, 0.0, 1.0);",
            "  floweryProgress = clampedProgress;",
            "  if (clampedProgress <= 0.0001) {",
            "    gl_FragColor = getFromColor(uv);",
            "    return;",
            "  }",
            "  if (clampedProgress >= 0.9999) {",
            "    gl_FragColor = getToColor(uv);",
            "    return;",
            "  }",
            "  gl_FragColor = transition(uv);",
            "}"
        ].join("\n");
    }

    function loadTransitionDefinition(entry) {
        if (!entry) {
            return Promise.reject(new Error("Transition not found"));
        }

        if (shaderCache.has(entry.path)) {
            return shaderCache.get(entry.path);
        }

        const promise = fetch(resolveAssetPath(entry.path))
            .then((response) => {
                if (!response.ok) {
                    throw new Error("Shader fetch failed: " + entry.path);
                }
                return response.text();
            })
            .then((source) => {
                const parsed = parseUniforms(source);
                return {
                    source,
                    uniforms: parsed.uniforms,
                    samplers: parsed.samplers
                };
            });

        shaderCache.set(entry.path, promise);
        return promise;
    }

    function applyUniformDefaults(gl, program, uniforms) {
        for (let i = 0; i < uniforms.length; i++) {
            const def = uniforms[i];
            if (def.name === "progress" || def.name === "ratio") {
                continue;
            }

            const location = gl.getUniformLocation(program, def.name);
            if (!location) {
                continue;
            }

            const value = def.value;
            switch (def.type) {
                case "float":
                    gl.uniform1f(location, value);
                    break;
                case "int":
                    gl.uniform1i(location, value);
                    break;
                case "bool":
                    gl.uniform1i(location, value ? 1 : 0);
                    break;
                case "vec2":
                    gl.uniform2f(location, value[0], value[1]);
                    break;
                case "vec3":
                    gl.uniform3f(location, value[0], value[1], value[2]);
                    break;
                case "vec4":
                    gl.uniform4f(location, value[0], value[1], value[2], value[3]);
                    break;
                case "ivec2":
                    gl.uniform2i(location, value[0], value[1]);
                    break;
                case "ivec3":
                    gl.uniform3i(location, value[0], value[1], value[2]);
                    break;
                case "ivec4":
                    gl.uniform4i(location, value[0], value[1], value[2], value[3]);
                    break;
                default:
                    break;
            }
        }
    }

    function setupExtraSamplers(gl, program, samplers, texture) {
        let unit = 2;
        for (let i = 0; i < samplers.length; i++) {
            const name = samplers[i];
            if (!name || name === "from" || name === "to") {
                continue;
            }

            const location = gl.getUniformLocation(program, name);
            if (!location) {
                continue;
            }

            gl.activeTexture(gl.TEXTURE0 + unit);
            gl.bindTexture(gl.TEXTURE_2D, texture);
            gl.uniform1i(location, unit);
            unit += 1;
        }
    }

    function applyTransitionDefinition(state, definition) {
        const gl = state.gl;
        const fs = buildFragmentShader(definition.source);
        const program = createProgram(gl, state.vsSource, fs);
        const bindQuad = createQuad(gl, program);
        const uFrom = gl.getUniformLocation(program, "from");
        const uTo = gl.getUniformLocation(program, "to");
        const uProgress = gl.getUniformLocation(program, "progress");
        const uRatio = gl.getUniformLocation(program, "ratio");

        gl.useProgram(program);

        if (uFrom) {
            gl.uniform1i(uFrom, 0);
        }
        if (uTo) {
            gl.uniform1i(uTo, 1);
        }

        applyUniformDefaults(gl, program, definition.uniforms);
        if (!state.extraTexture) {
            state.extraTexture = createSolidTexture(gl, 128, 128, 128, 255);
        }
        setupExtraSamplers(gl, program, definition.samplers, state.extraTexture);

        const previousProgram = state.program;
        state.program = program;
        state.bindQuad = bindQuad;
        state.uFrom = uFrom;
        state.uTo = uTo;
        state.uProgress = uProgress;
        state.uRatio = uRatio;
        state.definition = definition;

        if (previousProgram && previousProgram !== program) {
            gl.deleteProgram(previousProgram);
        }
    }

    function setCycleStart(state, imageIndex, nowMs) {
        const cycleSeconds = getTransitionCycleSeconds(state.transitionSec, state.intervalSec);
        const offsetMs = Number.isFinite(cycleSeconds) && cycleSeconds > 0
            ? imageIndex * cycleSeconds * 1000
            : 0;
        state.startTime = nowMs - offsetMs;
        state.lastCycleIndex = -1;
    }

    function initTransition(canvas, assets, transitionName, transitionSec, intervalSec, initialIndex) {
        const gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: false });
        if (!gl) {
            drawFallback(canvas, "Not supported (WebGL unavailable)");
            return null;
        }

        const assetsKey = assets ? JSON.stringify(assets) : "";
        const vsSource = [
            "attribute vec2 a_position;",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            "  v_texCoord = a_texCoord;",
            "  gl_Position = vec4(a_position, 0.0, 1.0);",
            "}"
        ].join("\n");

        const state = {
            canvas,
            gl,
            running: false,
            raf: 0,
            intervalSec: Number.isFinite(Number(intervalSec)) ? Number(intervalSec) : 0,
            baseTransitionSec: Number.isFinite(Number(transitionSec)) ? Number(transitionSec) : 0,
            transitionSec: normalizeTransitionSeconds(transitionSec, DEFAULT_TRANSITION_SEC, transitionName),
            defaultTransitionSec: DEFAULT_TRANSITION_SEC,
            assetsKey,
            transitionName: "",
            transitionEntry: null,
            program: null,
            bindQuad: null,
            uFrom: null,
            uTo: null,
            uProgress: null,
            uRatio: null,
            textures: [],
            extraTexture: null,
            startTime: null,
            lastCycleIndex: -1,
            fromTexture: null,
            toTexture: null,
            transitionRequestId: 0,
            vsSource,
            imageIndex: 0,
            alive: true,
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
                state.alive = false;
            },
            resize: function () {
                resizeCanvasToDisplaySize(canvas, gl);
            }
        };

        const images = resolveImages(assets);
        if (!images.list || images.list.length < 2) {
            drawFallback(canvas, "Not supported (assets missing)");
            return state;
        }

        const entry = resolveTransitionEntry(transitionName);
        if (!entry) {
            drawFallback(canvas, "Not supported (transition missing)");
            return state;
        }

        state.transitionName = entry.name;
        state.transitionEntry = entry;
        state.transitionSec = normalizeTransitionSeconds(state.baseTransitionSec, DEFAULT_TRANSITION_SEC, entry.name);

        state.updateTransition = function (nextName, nextTransitionSec, nextIntervalSec) {
            const nextEntry = resolveTransitionEntry(nextName);
            if (!nextEntry) {
                return;
            }

            const previousStartTime = state.startTime;
            const previousFromTexture = state.fromTexture;
            const previousToTexture = state.toTexture;
            const previousImageIndex = state.imageIndex;
            const previousIntervalSec = state.intervalSec;
            const previousTransitionSec = state.transitionSec;
            const previousTransitionName = state.transitionName;
            const transitionUpdateTime = performance.now();

            const nextInterval = Number(nextIntervalSec);
            state.intervalSec = Number.isFinite(nextInterval) ? nextInterval : 0;

            const nextTransitionValue = Number(nextTransitionSec);
            state.baseTransitionSec = Number.isFinite(nextTransitionValue) ? nextTransitionValue : 0;
            const fallback = Number.isFinite(state.defaultTransitionSec) ? state.defaultTransitionSec : DEFAULT_TRANSITION_SEC;
            state.transitionEntry = nextEntry;
            state.transitionName = nextEntry.name;
            state.transitionSec = normalizeTransitionSeconds(state.baseTransitionSec, fallback, nextEntry.name);

            const shouldPreserveCycle = previousStartTime !== null
                && previousFromTexture
                && previousToTexture
                && previousIntervalSec === state.intervalSec
                && previousTransitionSec === state.transitionSec
                && String(previousTransitionName || "").trim().toLowerCase() === String(nextEntry.name || "").trim().toLowerCase();
            const resolveCurrentIndex = (count) => {
                let baseIndex = Number.isFinite(previousImageIndex) ? previousImageIndex : 0;
                if (count <= 0 || previousStartTime === null) {
                    return baseIndex;
                }

                const elapsedSec = Math.max(0, (transitionUpdateTime - previousStartTime) * 0.001);
                const cycleSeconds = getTransitionCycleSeconds(previousTransitionSec, previousIntervalSec);
                if (cycleSeconds <= 0) {
                    return baseIndex;
                }

                const cycleIndex = Math.floor(elapsedSec / cycleSeconds);
                const fromIndex = ((cycleIndex % count) + count) % count;
                const toIndex = (fromIndex + 1) % count;
                const progress = computeTransitionProgress(elapsedSec, previousTransitionSec, previousIntervalSec);
                baseIndex = progress >= 0.5 ? toIndex : fromIndex;
                return baseIndex;
            };

            const requestId = ++state.transitionRequestId;
            loadTransitionDefinition(nextEntry).then((definition) => {
                if (!state.alive || state.transitionRequestId !== requestId) {
                    return;
                }

                try {
                    applyTransitionDefinition(state, definition);
                } catch (err) {
                    console.error("[CarouselGLTransitions] update failed", err);
                    return;
                }

                const imageCount = state.textures.length;
                if (imageCount > 0 && shouldPreserveCycle) {
                    state.imageIndex = ((previousImageIndex % imageCount) + imageCount) % imageCount;
                    state.fromTexture = previousFromTexture;
                    state.toTexture = previousToTexture;
                    state.startTime = previousStartTime;
                    return;
                }

                if (imageCount > 0) {
                    const baseIndex = resolveCurrentIndex(imageCount);
                    const normalizedIndex = ((baseIndex % imageCount) + imageCount) % imageCount;
                    state.imageIndex = normalizedIndex;
                    state.fromTexture = state.textures[normalizedIndex];
                    state.toTexture = state.textures[(normalizedIndex + 1) % imageCount] || state.fromTexture;
                    setCycleStart(state, normalizedIndex, transitionUpdateTime);
                } else {
                    setCycleStart(state, 0, performance.now());
                }
            }).catch((err) => {
                console.error("[CarouselGLTransitions] update failed", err);
            });
        };

        const imagePromises = images.list.map((path) => loadImage(path));
        Promise.all([loadTransitionDefinition(entry)].concat(imagePromises)).then((results) => {
            if (!state.alive) {
                return;
            }

            const definition = results[0];
            const loadedImages = results.slice(1);

            gl.clearColor(0, 0, 0, 0);
            state.textures = loadedImages.map((image) => createTexture(gl, image));

            try {
                applyTransitionDefinition(state, definition);
            } catch (err) {
                console.error("[CarouselGLTransitions] init failed", err);
                drawFallback(canvas, "Not supported (shader load failed)");
                return;
            }

            const imageCount = state.textures.length;
            const requestedIndex = Number(initialIndex);
            const normalizedIndex = imageCount > 0
                ? ((Number.isFinite(requestedIndex) ? Math.trunc(requestedIndex) : 0) % imageCount + imageCount) % imageCount
                : 0;
            state.imageIndex = normalizedIndex;
            state.fromTexture = imageCount > 0 ? state.textures[normalizedIndex] : null;
            state.toTexture = imageCount > 0
                ? state.textures[(normalizedIndex + 1) % imageCount] || state.fromTexture
                : null;
            setCycleStart(state, normalizedIndex, performance.now());

            startLoop(state, (time) => {
                if (!state.program || !state.bindQuad || state.textures.length === 0) {
                    return;
                }

                resizeCanvasToDisplaySize(canvas, gl);
                gl.clear(gl.COLOR_BUFFER_BIT);
                gl.useProgram(state.program);
                state.bindQuad();

                if (state.startTime === null) {
                    state.startTime = time;
                }

                const elapsed = (time - state.startTime) * 0.001;
                const progress = computeTransitionProgress(elapsed, state.transitionSec, state.intervalSec);
                if (state.uProgress) {
                    gl.uniform1f(state.uProgress, progress);
                }
                if (state.uRatio) {
                    const ratio = canvas.height > 0 ? canvas.width / canvas.height : 1;
                    gl.uniform1f(state.uRatio, ratio);
                }

                const textures = state.textures;
                let fromTexture = state.fromTexture || textures[0];
                let toTexture = state.toTexture || textures[1] || fromTexture;
                let fromIndex = state.imageIndex;
                let toIndex = textures.length > 0 ? (fromIndex + 1) % textures.length : 0;

                if (textures.length > 1) {
                    const cycleSeconds = getTransitionCycleSeconds(state.transitionSec, state.intervalSec);
                    const cycleIndex = cycleSeconds > 0 ? Math.floor(elapsed / cycleSeconds) : 0;
                    if (cycleIndex !== state.lastCycleIndex) {
                        state.lastCycleIndex = cycleIndex;
                        fromIndex = ((cycleIndex % textures.length) + textures.length) % textures.length;
                        toIndex = (fromIndex + 1) % textures.length;
                        fromTexture = textures[fromIndex];
                        toTexture = textures[toIndex];
                        state.imageIndex = fromIndex;
                        state.fromTexture = fromTexture;
                        state.toTexture = toTexture;
                    }
                }

                if (progress <= 0.0001) {
                    state.imageIndex = fromIndex;
                    state.fromTexture = fromTexture;
                    state.toTexture = toTexture;
                } else if (progress >= 0.9999) {
                    state.imageIndex = toIndex;
                    state.fromTexture = fromTexture;
                    state.toTexture = toTexture;
                }

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, fromTexture);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, toTexture);
                gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
            });
        }).catch((err) => {
            console.error("[CarouselGLTransitions] init failed", err);
            drawFallback(canvas, "Not supported (shader load failed)");
        });

        return state;
    }

    function stop(canvas) {
        if (!canvas || !canvas._floweryGlTransitions) {
            return;
        }
        const state = canvas._floweryGlTransitions;
        if (state.stop) {
            state.stop();
        }
        canvas._floweryGlTransitions = null;
    }

    function resize(canvas) {
        if (!canvas || !canvas._floweryGlTransitions) {
            return;
        }
        const state = canvas._floweryGlTransitions;
        if (state.resize) {
            state.resize();
        }
    }

    function init(canvas, assets, transitionName, transitionSec, intervalSec) {
        if (!canvas) {
            return;
        }

        const assetsKey = assets ? JSON.stringify(assets) : "";
        const previousState = canvas._floweryGlTransitions;
        if (previousState
            && previousState.alive
            && previousState.assetsKey === assetsKey
            && typeof previousState.updateTransition === "function") {
            previousState.updateTransition(transitionName, transitionSec, intervalSec);
            return;
        }

        const previousIndex = previousState && Number.isFinite(previousState.imageIndex)
            ? previousState.imageIndex
            : 0;

        stop(canvas);

        let state = null;
        try {
            state = initTransition(canvas, assets, transitionName, transitionSec, intervalSec, previousIndex);
        } catch (err) {
            console.error("[CarouselGLTransitions] init failed", err);
        }

        if (state) {
            canvas._floweryGlTransitions = state;
        }
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
            assetsKey: null,
            transitionName: null,
            intervalSec: 0,
            transitionSec: 0
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

    function attachOverlay(key, left, top, width, height, assets, transitionName, transitionSec, intervalSec) {
        if (!key) {
            return;
        }

        const entry = ensureOverlayCanvas(key);
        const assetsKey = assets ? JSON.stringify(assets) : "";
        const transitionKey = transitionName || "";
        const transitionValue = Number(transitionSec);
        const nextTransitionSec = Number.isFinite(transitionValue) ? transitionValue : 0;
        const intervalValue = Number(intervalSec);
        const nextIntervalSec = Number.isFinite(intervalValue) ? intervalValue : 0;

        if (entry.assetsKey !== assetsKey || entry.transitionName !== transitionKey) {
            entry.assetsKey = assetsKey;
            entry.transitionName = transitionKey;
            entry.transitionSec = nextTransitionSec;
            entry.intervalSec = nextIntervalSec;
            init(entry.canvas, assets || null, transitionName || null, nextTransitionSec, nextIntervalSec);
        } else if (entry.transitionSec !== nextTransitionSec || entry.intervalSec !== nextIntervalSec) {
            entry.transitionSec = nextTransitionSec;
            entry.intervalSec = nextIntervalSec;
            setOverlayTransitionDuration(key, nextTransitionSec);
            setOverlayTiming(key, nextIntervalSec);
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
        if (entry.canvas && entry.canvas._floweryGlTransitions) {
            entry.canvas._floweryGlTransitions.intervalSec = entry.intervalSec;
        }
    }

    function setOverlayTransitionDuration(key, transitionSec) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        const seconds = Number(transitionSec);
        entry.transitionSec = Number.isFinite(seconds) ? seconds : 0;
        if (entry.canvas && entry.canvas._floweryGlTransitions) {
            const state = entry.canvas._floweryGlTransitions;
            const fallback = Number.isFinite(state.defaultTransitionSec) ? state.defaultTransitionSec : DEFAULT_TRANSITION_SEC;
            state.baseTransitionSec = entry.transitionSec;
            state.transitionSec = normalizeTransitionSeconds(entry.transitionSec, fallback, state.transitionName);
        }
    }

    function setOverlayActive(key, isActive) {
        const entry = overlayMap.get(key);
        if (!entry) {
            return;
        }

        if (isActive) {
            entry.canvas.style.display = "block";
            if (entry.canvas._floweryGlTransitions && entry.canvas._floweryGlTransitions.resume) {
                entry.canvas._floweryGlTransitions.resume();
            }
            resize(entry.canvas);
        } else {
            if (entry.canvas._floweryGlTransitions && entry.canvas._floweryGlTransitions.pause) {
                entry.canvas._floweryGlTransitions.pause();
            }
            entry.canvas.style.display = "none";
        }
    }

    window.FloweryCarouselGLTransitions = {
        init,
        stop,
        resize,
        attachOverlay,
        detachOverlay,
        detachAllOverlays,
        setOverlayTiming,
        setOverlayTransitionDuration,
        setOverlayActive
    };
})();
