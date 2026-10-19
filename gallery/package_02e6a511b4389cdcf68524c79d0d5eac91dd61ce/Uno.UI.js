var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Media;
            (function (Media) {
                class CompositionTarget {
                    static async buildImports() {
                        if (CompositionTarget.requestRender === undefined) {
                            const exports = await window.Module.getAssemblyExports("Uno.UI");
                            CompositionTarget.requestRender = () => exports.Microsoft.UI.Xaml.Media.CompositionTarget.FrameCallback();
                        }
                    }
                    static requestFrame() {
                        CompositionTarget.buildImports().then(() => {
                            window.requestAnimationFrame(CompositionTarget.requestRender);
                        });
                    }
                }
                Media.CompositionTarget = CompositionTarget;
            })(Media = Xaml.Media || (Xaml.Media = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
// eslint-disable-next-line @typescript-eslint/no-namespace
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class ExportManager {
            static async initialize() {
                await Windows.ApplicationModel.Core.CoreApplication.initializeExports();
                if (Module.getAssemblyExports !== undefined) {
                    const unoUIExports = await Module.getAssemblyExports("Uno.UI");
                    if (Object.entries(unoUIExports).length > 0) {
                        // DotnetExports may already have been initialized
                        globalThis.DotnetExports = globalThis.DotnetExports || {};
                        globalThis.DotnetExports.UnoUI = unoUIExports;
                    }
                }
            }
        }
        UI.ExportManager = ExportManager;
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var Utils;
    (function (Utils) {
        class Guid {
            static NewGuid() {
                if (!Guid.newGuidMethod) {
                    Guid.newGuidMethod = Module.mono_bind_static_method("[mscorlib] System.Guid:NewGuid");
                }
                return Guid.newGuidMethod();
            }
        }
        Utils.Guid = Guid;
    })(Utils = Uno.Utils || (Uno.Utils = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class HtmlDom {
            /**
             * Initialize various polyfills used by Uno
             */
            static initPolyfills() {
                this.isConnectedPolyfill();
            }
            static isConnectedPolyfill() {
                function get() {
                    // polyfill implementation
                    return document.contains(this);
                }
                (supported => {
                    if (!supported) {
                        Object.defineProperty(Node.prototype, "isConnected", { get });
                    }
                })("isConnected" in Node.prototype);
            }
        }
        UI.HtmlDom = HtmlDom;
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        let HtmlEventDispatchResult;
        (function (HtmlEventDispatchResult) {
            HtmlEventDispatchResult[HtmlEventDispatchResult["Ok"] = 0] = "Ok";
            HtmlEventDispatchResult[HtmlEventDispatchResult["StopPropagation"] = 1] = "StopPropagation";
            HtmlEventDispatchResult[HtmlEventDispatchResult["PreventDefault"] = 2] = "PreventDefault";
            HtmlEventDispatchResult[HtmlEventDispatchResult["NotDispatched"] = 128] = "NotDispatched";
        })(HtmlEventDispatchResult = UI.HtmlEventDispatchResult || (UI.HtmlEventDispatchResult = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// eslint-disable-next-line @typescript-eslint/no-namespace
var MonoSupport;
(function (MonoSupport) {
    /**
     * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
     * unmarshaled invocation of javascript from .NET code.
     * */
    class jsCallDispatcher {
        /**
         * Registers a instance for a specified identier
         * @param identifier the scope name
         * @param instance the instance to use for the scope
         */
        static registerScope(identifier, instance) {
            jsCallDispatcher.registrations.set(identifier, instance);
        }
        static invokeJSUnmarshalled(funcName, arg0, arg1, arg2) {
            const funcInstance = jsCallDispatcher.findJSFunction(funcName);
            let ret = funcInstance.call(null, arg0, arg1, arg2);
            switch (typeof ret) {
                case "boolean":
                    return ret ? 1 : 0;
                case "undefined":
                    return 0;
                case "number":
                    return ret;
                default:
                    throw new Error(`Function ${funcName} returned an unsupported type: ${typeof ret}`);
            }
        }
        static findJSFunction(identifier) {
            if (!identifier) {
                return jsCallDispatcher.dispatch;
            }
            else {
                if (!jsCallDispatcher._isUnoRegistered) {
                    jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_StorageFolder", Windows.Storage.StorageFolder);
                    jsCallDispatcher.registerScope("UnoStatic_Windows_Storage_ApplicationDataContainer", Windows.Storage.ApplicationDataContainer);
                    jsCallDispatcher.registerScope("UnoStatic_Windows_ApplicationModel_DataTransfer_DragDrop_Core_DragDropExtension", Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension);
                    jsCallDispatcher._isUnoRegistered = true;
                }
                const { ns, methodName } = jsCallDispatcher.parseIdentifier(identifier);
                var instance = jsCallDispatcher.registrations.get(ns);
                if (instance) {
                    var boundMethod = instance[methodName].bind(instance);
                    var methodId = jsCallDispatcher.cacheMethod(boundMethod);
                    return () => methodId;
                }
                else {
                    throw `Unknown scope ${ns}`;
                }
            }
        }
        /**
         * Internal dispatcher for methods invoked through TSInteropMarshaller
         * @param id The method ID obtained when invoking WebAssemblyRuntime.InvokeJSUnmarshalled with a method name
         * @param pParams The parameters structure ID
         * @param pRet The pointer to the return value structure
         */
        static dispatch(id, pParams, pRet) {
            return jsCallDispatcher.methodMap[id + ""](pParams, pRet);
        }
        /**
         * Parses the method identifier
         * @param identifier
         */
        static parseIdentifier(identifier) {
            var parts = identifier.split(':');
            const ns = parts[0];
            const methodName = parts[1];
            return { ns, methodName };
        }
        /**
         * Adds the a resolved method for a given identifier
         * @param identifier the findJSFunction identifier
         * @param boundMethod the method to call
         */
        static cacheMethod(boundMethod) {
            var methodId = Object.keys(jsCallDispatcher.methodMap).length;
            jsCallDispatcher.methodMap[methodId + ""] = boundMethod;
            return methodId;
        }
        static getMethodMapId(methodHandle) {
            return methodHandle + "";
        }
        static invokeOnMainThread() {
            if (!jsCallDispatcher.dispatcherCallback) {
                jsCallDispatcher.dispatcherCallback = globalThis.DotnetExports.UnoUIDispatching.Uno.UI.Dispatching.NativeDispatcher.DispatcherCallback;
            }
            // Use setImmediate to return avoid blocking the background thread
            // on a sync call.
            window.setImmediate(() => {
                try {
                    jsCallDispatcher.dispatcherCallback();
                }
                catch (e) {
                    console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
                    throw e;
                }
            });
        }
    }
    jsCallDispatcher.registrations = new Map();
    jsCallDispatcher.methodMap = {};
    MonoSupport.jsCallDispatcher = jsCallDispatcher;
})(MonoSupport || (MonoSupport = {}));
// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
window.DotNet = MonoSupport;
// Export the main thread invoker for threading support
MonoSupport.invokeOnMainThread = MonoSupport.jsCallDispatcher.invokeOnMainThread;
// eslint-disable-next-line @typescript-eslint/no-namespace
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class WindowManager {
            constructor(containerElementId, loadingElementId) {
                this.containerElementId = containerElementId;
                this.loadingElementId = loadingElementId;
                this.allActiveElementsById = {};
                /** Native elements created with the BrowserHtmlElement class */
                this.nativeHandlersMap = {};
                this.uiElementRegistrations = {};
                this.initDom();
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static async init(containerElementId = "uno-body", loadingElementId = "uno-loading") {
                UI.HtmlDom.initPolyfills();
                await WindowManager.initMethods();
                Uno.UI.Dispatching.NativeDispatcher.init(WindowManager.buildReadyPromise());
                this.current = new WindowManager(containerElementId, loadingElementId);
                MonoSupport.jsCallDispatcher.registerScope("Uno", this.current);
                this.current.init();
            }
            /**
             * Builds a promise that will signal the ability for the dispatcher
             * to initiate work.
             * */
            static buildReadyPromise() {
                return new Promise(resolve => {
                    Promise.all([WindowManager.buildSplashScreen()]).then(() => resolve(true));
                });
            }
            /**
             * Build the splashscreen image eagerly
             * */
            static buildSplashScreen() {
                return new Promise(resolve => {
                    let bootstrapperLoaders = document.getElementsByClassName(WindowManager.unoPersistentLoaderClassName);
                    if (bootstrapperLoaders.length > 0) {
                        // Bootstrapper supports persistent loader, skip creating local one and keep it displayed
                        let bootstrapperLoader = bootstrapperLoaders[0];
                        bootstrapperLoader.classList.add(WindowManager.unoKeepLoaderClassName);
                        resolve(true);
                    }
                    else {
                        const img = new Image();
                        let loaded = false;
                        let loadingDone = () => {
                            if (!loaded) {
                                loaded = true;
                                if (img.width !== 0 && img.height !== 0) {
                                    // Materialize the image content so it shows immediately
                                    // even if the dispatcher is blocked thereafter by all
                                    // the Uno initialization work. The resulting canvas is not used.
                                    //
                                    // If the image fails to load, setup the splashScreen anyways with the
                                    // proper sample.
                                    let canvas = document.createElement("canvas");
                                    canvas.width = img.width;
                                    canvas.height = img.height;
                                    let ctx = canvas.getContext("2d");
                                    ctx.drawImage(img, 0, 0);
                                }
                                if (document.readyState === "loading") {
                                    document.addEventListener("DOMContentLoaded", () => {
                                        WindowManager.setupSplashScreen(img);
                                        resolve(true);
                                    });
                                }
                                else {
                                    WindowManager.setupSplashScreen(img);
                                    resolve(true);
                                }
                            }
                        };
                        // Preload the splash screen so the image element
                        // created later on 
                        img.onload = loadingDone;
                        img.onerror = loadingDone;
                        const UNO_BOOTSTRAP_APP_BASE = config.environmentVariables["UNO_BOOTSTRAP_APP_BASE"] || "";
                        const UNO_BOOTSTRAP_WEBAPP_BASE_PATH = config.environmentVariables["UNO_BOOTSTRAP_WEBAPP_BASE_PATH"] || "";
                        let fullImagePath = String(UnoAppManifest.splashScreenImage);
                        // If the splashScreenImage image already points to the app base path, use it, otherwise we build it.
                        if (UNO_BOOTSTRAP_APP_BASE !== "" && fullImagePath.indexOf(UNO_BOOTSTRAP_APP_BASE) == -1) {
                            fullImagePath = `${UNO_BOOTSTRAP_WEBAPP_BASE_PATH}${UNO_BOOTSTRAP_APP_BASE}/${UnoAppManifest.splashScreenImage}`;
                        }
                        img.src = fullImagePath;
                        // If there's no response, skip the loading
                        setTimeout(loadingDone, 2000);
                    }
                });
            }
            /**
                * Creates the UWP-compatible splash screen
                *
                */
            static setupSplashScreen(splashImage) {
                if (UnoAppManifest && UnoAppManifest.splashScreenImage) {
                    const unoBody = document.getElementById("uno-body");
                    if (unoBody) {
                        const unoLoading = document.createElement("div");
                        unoLoading.id = "uno-loading";
                        if (UnoAppManifest.lightThemeBackgroundColor) {
                            unoLoading.style.setProperty("--light-theme-bg-color", UnoAppManifest.lightThemeBackgroundColor);
                        }
                        if (UnoAppManifest.darkThemeBackgroundColor) {
                            unoLoading.style.setProperty("--dark-theme-bg-color", UnoAppManifest.darkThemeBackgroundColor);
                        }
                        if (UnoAppManifest.splashScreenColor && UnoAppManifest.splashScreenColor != 'transparent') {
                            unoLoading.style.backgroundColor = UnoAppManifest.splashScreenColor;
                        }
                        splashImage.id = "uno-loading-splash";
                        splashImage.classList.add("uno-splash");
                        unoLoading.appendChild(splashImage);
                        unoBody.appendChild(unoLoading);
                    }
                    const loading = document.getElementById("loading");
                    if (loading) {
                        loading.remove();
                    }
                }
            }
            static setBodyCursor(value) {
                document.body.style.cursor = value;
            }
            static setSingleLine(htmlId) {
                const element = this.current.getView(htmlId);
                if (element instanceof HTMLTextAreaElement) {
                    element.addEventListener("keydown", e => {
                        if (e.key === "Enter") {
                            e.preventDefault();
                        }
                    });
                }
            }
            /**
                * Reads the window's search parameters
                *
                */
            static beforeLaunch() {
                WindowManager.resize();
                if (typeof URLSearchParams === "function") {
                    return new URLSearchParams(window.location.search).toString();
                }
                else {
                    const queryIndex = document.location.search.indexOf("?");
                    if (queryIndex !== -1) {
                        return document.location.search.substring(queryIndex + 1);
                    }
                    return "";
                }
            }
            /**
                * Estimated application startup time
                */
            static getBootTime() {
                return Date.now() - performance.now();
            }
            containsPoint(htmlId, x, y, considerFill, considerStroke) {
                const view = this.getView(htmlId);
                if (view instanceof SVGGeometryElement) {
                    try {
                        const point = new DOMPoint(x, y);
                        return (considerFill && view.isPointInFill(point)) ||
                            (considerStroke && view.isPointInStroke(point));
                    }
                    catch (e) {
                        // SVGPoint is deprecated, but only Firefox and Safari supports DOMPoint
                        const svgElement = view.closest("svg");
                        const point = svgElement.createSVGPoint();
                        point.x = x;
                        point.y = y;
                        return (considerFill && view.isPointInFill(point)) ||
                            (considerStroke && view.isPointInStroke(point));
                    }
                }
                return false;
            }
            /**
                * Create a html DOM element representing a Xaml element.
                *
                * You need to call addView to connect it to the DOM.
                */
            createContentNativeFast(htmlId, tagName, uiElementRegistrationId, isFocusable, isSvg) {
                this.createContentInternal({
                    id: this.handleToString(htmlId),
                    handle: htmlId,
                    tagName: tagName,
                    uiElementRegistrationId: uiElementRegistrationId,
                    isFocusable: isFocusable,
                    isSvg: isSvg
                });
            }
            createContentInternal(contentDefinition) {
                // Create the HTML element
                const element = contentDefinition.isSvg
                    ? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
                    : document.createElement(contentDefinition.tagName);
                element.id = contentDefinition.id;
                const uiElementRegistration = this.uiElementRegistrations[this.handleToString(contentDefinition.uiElementRegistrationId)];
                if (!uiElementRegistration) {
                    throw `UIElement registration id ${contentDefinition.uiElementRegistrationId} is unknown.`;
                }
                element.setAttribute("XamlType", uiElementRegistration.typeName);
                element.setAttribute("XamlHandle", this.handleToString(contentDefinition.handle));
                if (uiElementRegistration.isFrameworkElement) {
                    this.setAsUnarranged(element, true);
                }
                if (element.hasOwnProperty("tabindex")) {
                    element["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
                }
                else {
                    element.setAttribute("tabindex", contentDefinition.isFocusable ? "0" : "-1");
                }
                if (contentDefinition) {
                    let classes = element.classList.value;
                    for (const className of uiElementRegistration.classNames) {
                        classes += " uno-" + className;
                    }
                    element.classList.value = classes;
                }
                // Add the html element to list of elements
                this.allActiveElementsById[contentDefinition.id] = element;
            }
            registerUIElement(typeName, isFrameworkElement, classNames) {
                const registrationId = Object.keys(this.uiElementRegistrations).length;
                this.uiElementRegistrations[this.handleToString(registrationId)] = {
                    classNames: classNames,
                    isFrameworkElement: isFrameworkElement,
                    typeName: typeName,
                };
                return registrationId;
            }
            getView(elementHandle) {
                const element = this.allActiveElementsById[this.handleToString(elementHandle)];
                if (!element) {
                    throw `Element id ${elementHandle} not found.`;
                }
                return element;
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setNameNative(pParam) {
                const params = WindowManagerSetNameParams.unmarshal(pParam);
                this.setNameInternal(params.HtmlId, params.Name);
                return true;
            }
            setNameInternal(elementId, name) {
                this.getView(elementId).setAttribute("xamlname", name);
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setXUidNative(pParam) {
                const params = WindowManagerSetXUidParams.unmarshal(pParam);
                this.setXUidInternal(params.HtmlId, params.Uid);
                return true;
            }
            setXUidInternal(elementId, name) {
                this.getView(elementId).setAttribute("xuid", name);
            }
            setVisibilityNativeFast(htmlId, visible) {
                this.setVisibilityInternal(htmlId, visible);
            }
            setVisibilityInternal(elementId, visible) {
                const element = this.getView(elementId);
                if (visible) {
                    element.classList.remove(WindowManager.unoCollapsedClassName);
                }
                else {
                    element.classList.add(WindowManager.unoCollapsedClassName);
                }
            }
            /**
                * Set an attribute for an element.
                */
            setAttributesNativeFast(htmlId, pairs) {
                const element = this.getView(htmlId);
                const length = pairs.length;
                for (let i = 0; i < length; i += 2) {
                    element.setAttribute(pairs[i], pairs[i + 1]);
                }
            }
            /**
                * Set an attribute for an element.
                */
            setAttribute(htmlId, name, value) {
                const element = this.getView(htmlId);
                element.setAttribute(name, value);
            }
            /**
                * Removes an attribute for an element.
                */
            removeAttributeNative(pParams) {
                const params = WindowManagerRemoveAttributeParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.removeAttribute(params.Name);
                return true;
            }
            /**
                * Get an attribute for an element.
                */
            getAttribute(elementId, name) {
                return this.getView(elementId).getAttribute(name);
            }
            /**
                * Set a property for an element.
                */
            setPropertyNativeFast(htmlId, pairs) {
                const element = this.getView(htmlId);
                const length = pairs.length;
                for (let i = 0; i < length; i += 2) {
                    const setVal = pairs[i + 1];
                    if (setVal === "true") {
                        element[pairs[i]] = true;
                    }
                    else if (setVal === "false") {
                        element[pairs[i]] = false;
                    }
                    else {
                        element[pairs[i]] = setVal;
                    }
                }
            }
            setSinglePropertyNativeFast(htmlId, name, value) {
                const element = this.getView(htmlId);
                if (value === "true") {
                    element[name] = true;
                }
                else if (value === "false") {
                    element[name] = false;
                }
                else {
                    element[name] = value;
                }
            }
            /**
                * Get a property for an element.
                */
            getProperty(elementId, name) {
                const element = this.getView(elementId);
                return (element[name] || "").toString();
            }
            /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
            setStyleNativeFast(htmlId, styles) {
                const elementStyle = this.getView(htmlId).style;
                const stylesLength = styles.length;
                for (let i = 0; i < stylesLength; i += 2) {
                    elementStyle.setProperty(styles[i], styles[i + 1]);
                }
            }
            /**
            * Set a single CSS style of a html element
            *
            */
            setStyleDoubleNative(pParams) {
                const params = WindowManagerSetStyleDoubleParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                element.style.setProperty(params.Name, this.handleToString(params.Value));
                return true;
            }
            setStyleStringNativeFast(htmlId, name, value) {
                this.getView(htmlId).style.setProperty(name, value);
            }
            /**
                * Remove the CSS style of a html element.
                */
            resetStyle(elementId, names) {
                const element = this.getView(elementId);
                for (const name of names) {
                    element.style.setProperty(name, "");
                }
            }
            isCssConditionSupported(supportCondition) {
                return CSS.supports(supportCondition);
            }
            /**
             * Set + Unset CSS classes on an element
             */
            setUnsetCssClasses(elementId, cssClassesToSet, cssClassesToUnset) {
                const element = this.getView(elementId);
                if (cssClassesToSet) {
                    cssClassesToSet.forEach(c => {
                        element.classList.add(c);
                    });
                }
                if (cssClassesToUnset) {
                    cssClassesToUnset.forEach(c => {
                        element.classList.remove(c);
                    });
                }
            }
            /**
             * Set CSS classes on an element from a specified list
             */
            setClasses(elementId, cssClassesList, classIndex) {
                const element = this.getView(elementId);
                for (let i = 0; i < cssClassesList.length; i++) {
                    if (i === classIndex) {
                        element.classList.add(cssClassesList[i]);
                    }
                    else {
                        element.classList.remove(cssClassesList[i]);
                    }
                }
            }
            /**
            * Arrange and clips a native elements
            *
            */
            arrangeElementNativeFast(htmlId, top, left, width, height, clip, clipTop, clipLeft, clipBottom, clipRight) {
                const element = this.getView(htmlId);
                const style = element.style;
                style.position = "absolute";
                style.top = top + "px";
                style.left = left + "px";
                style.width = width === NaN ? "auto" : width + "px";
                style.height = height === NaN ? "auto" : height + "px";
                if (clip) {
                    style.clip = `rect(${clipTop}px, ${clipRight}px, ${clipBottom}px, ${clipLeft}px)`;
                }
                else {
                    style.clip = "";
                }
                this.setAsArranged(element);
            }
            setAsArranged(element) {
                if (!element._unoIsArranged) {
                    element._unoIsArranged = true;
                    element.classList.remove(WindowManager.unoUnarrangedClassName);
                }
            }
            setAsUnarranged(element, force = false) {
                if (element._unoIsArranged || force) {
                    element._unoIsArranged = false;
                    element.classList.add(WindowManager.unoUnarrangedClassName);
                }
            }
            /**
            * Sets the color property of the specified element
            */
            setElementColorNative(pParam) {
                const params = WindowManagerSetElementColorParams.unmarshal(pParam);
                this.setElementColorInternal(params.HtmlId, params.Color);
                return true;
            }
            setElementColorInternal(elementId, color) {
                const element = this.getView(elementId);
                element.style.setProperty("color", this.numberToCssColor(color));
            }
            /**
             * Sets the element's selection highlight.
            **/
            setSelectionHighlight(elementId, backgroundColor, foregroundColor) {
                const element = this.getView(elementId);
                element.classList.add("selection-highlight");
                element.style.setProperty("--selection-background", this.numberToCssColor(backgroundColor));
                element.style.setProperty("--selection-color", this.numberToCssColor(foregroundColor));
                return true;
            }
            setSelectionHighlightNative(pParam) {
                const params = WindowManagerSetSelectionHighlightParams.unmarshal(pParam);
                return this.setSelectionHighlight(params.HtmlId, params.BackgroundColor, params.ForegroundColor);
            }
            /**
            * Sets the background color property of the specified element
            */
            setElementBackgroundColor(pParam) {
                const params = WindowManagerSetElementBackgroundColorParams.unmarshal(pParam);
                const element = this.getView(params.HtmlId);
                const style = element.style;
                style.setProperty("background-color", this.numberToCssColor(params.Color));
                style.removeProperty("background-image");
                return true;
            }
            /**
            * Sets the background image property of the specified element
            */
            setElementBackgroundGradient(pParam) {
                const params = WindowManagerSetElementBackgroundGradientParams.unmarshal(pParam);
                const element = this.getView(params.HtmlId);
                const style = element.style;
                style.removeProperty("background-color");
                style.setProperty("background-image", params.CssGradient);
                return true;
            }
            /**
            * Clears the background property of the specified element
            */
            resetElementBackground(pParam) {
                const params = WindowManagerResetElementBackgroundParams.unmarshal(pParam);
                const element = this.getView(params.HtmlId);
                const style = element.style;
                style.removeProperty("background-color");
                style.removeProperty("background-image");
                style.removeProperty("background-size");
                return true;
            }
            /**
            * Sets the transform matrix of an element
            *
            */
            setElementTransformNativeFast(htmlId, m11, m12, m21, m22, m31, m32) {
                const element = this.getView(htmlId);
                element.style.transform = `matrix(${m11},${m12},${m21},${m22},${m31},${m32})`;
                this.setAsArranged(element);
            }
            setPointerEvents(htmlId, enabled) {
                this.getView(htmlId).style.pointerEvents = enabled ? "auto" : "none";
            }
            /**
                * Issue a browser alert to user
                * @param message message to display
                */
            alert(message) {
                window.alert(message);
                return "ok";
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewNative(pParams) {
                const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);
                this.registerEventOnViewInternal(params.HtmlId, params.EventName, params.OnCapturePhase, params.EventExtractorId);
                return true;
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewInternal(elementId, eventName, onCapturePhase = false, eventExtractorId) {
                const element = this.getView(elementId);
                const eventExtractor = this.getEventExtractor(eventExtractorId);
                const eventHandler = (event) => {
                    const eventPayload = eventExtractor
                        ? `${eventExtractor(event)}`
                        : "";
                    const result = this.dispatchEvent(element, eventName, eventPayload, onCapturePhase);
                    if (result & UI.HtmlEventDispatchResult.StopPropagation) {
                        event.stopPropagation();
                    }
                    if (result & UI.HtmlEventDispatchResult.PreventDefault) {
                        event.preventDefault();
                    }
                };
                element.addEventListener(eventName, eventHandler, onCapturePhase);
            }
            /**
             * keyboard event extractor to be used with registerEventOnView
             * @param evt
             */
            keyboardEventExtractor(evt) {
                return (evt instanceof KeyboardEvent) ? `${(evt.ctrlKey ? "1" : "0")}${(evt.altKey ? "1" : "0")}${(evt.metaKey ? "1" : "0")}${(evt.shiftKey ? "1" : "0")}${evt.key}` : "0";
            }
            /**
             * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
             * @param evt
             */
            tappedEventExtractor(evt) {
                return evt
                    ? `0;${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};mouse`
                    : "";
            }
            /**
             * focus event extractor to be used with registerEventOnView
             * @param evt
             */
            focusEventExtractor(evt) {
                if (evt) {
                    const targetElement = evt.target;
                    if (targetElement) {
                        const targetXamlHandle = targetElement.getAttribute("XamlHandle");
                        if (targetXamlHandle) {
                            return `${targetXamlHandle}`;
                        }
                    }
                }
                return "";
            }
            customEventDetailExtractor(evt) {
                if (evt) {
                    const detail = evt.detail;
                    if (detail) {
                        return JSON.stringify(detail);
                    }
                }
                return "";
            }
            customEventDetailStringExtractor(evt) {
                return evt ? `${evt.detail}` : "";
            }
            /**
             * Gets the event extractor function. See UIElement.HtmlEventExtractor
             * @param eventExtractorName an event extractor name.
             */
            getEventExtractor(eventExtractorId) {
                if (eventExtractorId) {
                    //
                    // NOTE TO MAINTAINERS: Keep in sync with Microsoft.UI.Xaml.UIElement.HtmlEventExtractor
                    //
                    switch (eventExtractorId) {
                        case 3:
                            return this.keyboardEventExtractor;
                        case 2:
                            return this.tappedEventExtractor;
                        case 4:
                            return this.focusEventExtractor;
                        case 6:
                            return this.customEventDetailExtractor;
                        case 5:
                            return this.customEventDetailStringExtractor;
                    }
                    throw `Event extractor ${eventExtractorId} is not supported`;
                }
                return null;
            }
            /**
                * Set or replace the root element.
                */
            setRootElement(elementId) {
                if (this.rootElement && Number(this.rootElement.id) === elementId) {
                    return null; // nothing to do
                }
                if (this.rootElement) {
                    // Remove existing
                    this.containerElement.removeChild(this.rootElement);
                    this.rootElement.classList.remove(WindowManager.unoRootClassName);
                }
                if (!elementId) {
                    return null;
                }
                // set new root
                const newRootElement = this.getView(elementId);
                newRootElement.classList.add(WindowManager.unoRootClassName);
                this.rootElement = newRootElement;
                this.containerElement.appendChild(this.rootElement);
                this.setAsArranged(newRootElement); // patch because root is not measured/arranged
            }
            /**
                * Set a view as a child of another one.
                * @param pParams Pointer to a WindowManagerAddViewParams native structure.
                */
            addViewNative(pParams) {
                const params = WindowManagerAddViewParams.unmarshal(pParams);
                this.addViewInternal(params.HtmlId, params.ChildView, params.Index != -1 ? params.Index : null);
                return true;
            }
            addViewInternal(parentId, childId, index) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                if (index != null && index < parentElement.childElementCount) {
                    const insertBeforeElement = parentElement.children[index];
                    parentElement.insertBefore(childElement, insertBeforeElement);
                }
                else {
                    parentElement.appendChild(childElement);
                }
            }
            /**
                * Remove a child from a parent element.
                */
            removeViewNative(pParams) {
                const params = WindowManagerRemoveViewParams.unmarshal(pParams);
                this.removeViewInternal(params.HtmlId, params.ChildView);
                return true;
            }
            removeViewInternal(parentId, childId) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                parentElement.removeChild(childElement);
                // Mark the element as unarranged, so if it gets measured while being
                // disconnected from the root element, it won't be visible.
                this.setAsUnarranged(childElement);
            }
            destroyViewNativeFast(htmlId) {
                this.destroyViewInternal(htmlId);
            }
            destroyViewInternal(elementId) {
                const element = this.getView(elementId);
                if (element.parentElement) {
                    element.parentElement.removeChild(element);
                }
                delete this.allActiveElementsById[elementId];
            }
            getBBox(elementId) {
                const element = this.getView(elementId);
                let unconnectedRoot = null;
                const cleanupUnconnectedRoot = (owner) => {
                    if (unconnectedRoot !== null) {
                        owner.removeChild(unconnectedRoot);
                    }
                };
                try {
                    // On FireFox, the element needs to be connected to the DOM
                    // or the getBBox() will crash.
                    if (!element.isConnected) {
                        unconnectedRoot = element;
                        while (unconnectedRoot.parentElement) {
                            // Need to find the top most "unconnected" parent
                            // of this element
                            unconnectedRoot = unconnectedRoot.parentElement;
                        }
                        this.containerElement.appendChild(unconnectedRoot);
                    }
                    let bbox = element.getBBox();
                    return [
                        bbox.x,
                        bbox.y,
                        bbox.width,
                        bbox.height
                    ];
                }
                finally {
                    cleanupUnconnectedRoot(this.containerElement);
                }
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureViewNativeFast(htmlId, availableWidth, availableHeight, measureContent, pReturn) {
                const result = this.measureViewInternal(htmlId, availableWidth, availableHeight, measureContent);
                const desiredSize = new WindowManagerMeasureViewReturn();
                desiredSize.DesiredWidth = result[0];
                desiredSize.DesiredHeight = result[1];
                desiredSize.marshal(pReturn);
            }
            measureElement(element) {
                const offsetWidth = element.offsetWidth;
                const offsetHeight = element.offsetHeight;
                const resultWidth = offsetWidth ? offsetWidth : element.clientWidth;
                const resultHeight = offsetHeight ? offsetHeight : element.clientHeight;
                // +1 is added to take rounding/flooring into account
                return [resultWidth + 1, resultHeight];
            }
            measureViewInternal(viewId, maxWidth, maxHeight, measureContent) {
                const element = this.getView(viewId);
                const elementStyle = element.style;
                const elementClasses = element.className;
                const originalStyleCssText = elementStyle.cssText;
                const unconstrainedStyleCssText = this.createUnconstrainedStyle(elementStyle, maxWidth, maxHeight);
                let parentElement = null;
                let parentElementWidthHeight = null;
                let unconnectedRoot = null;
                const cleanupUnconnectedRoot = (owner) => {
                    if (unconnectedRoot !== null) {
                        owner.removeChild(unconnectedRoot);
                    }
                };
                try {
                    if (!element.isConnected) {
                        // If the element is not connected to the DOM, we need it
                        // to be connected for the measure to provide a meaningful value.
                        unconnectedRoot = element;
                        while (unconnectedRoot.parentElement) {
                            // Need to find the top most "unconnected" parent
                            // of this element
                            unconnectedRoot = unconnectedRoot.parentElement;
                        }
                        this.containerElement.appendChild(unconnectedRoot);
                    }
                    if (measureContent && element instanceof HTMLImageElement) {
                        elementStyle.cssText = unconstrainedStyleCssText;
                        const imgElement = element;
                        return [imgElement.naturalWidth, imgElement.naturalHeight];
                    }
                    else if (measureContent && element instanceof HTMLInputElement) {
                        elementStyle.cssText = unconstrainedStyleCssText;
                        const inputElement = element;
                        cleanupUnconnectedRoot(this.containerElement);
                        // Create a temporary element that will contain the input's content
                        const textOnlyElement = document.createElement("p");
                        textOnlyElement.style.cssText = unconstrainedStyleCssText;
                        textOnlyElement.innerText = inputElement.value;
                        textOnlyElement.className = elementClasses;
                        unconnectedRoot = textOnlyElement;
                        this.containerElement.appendChild(unconnectedRoot);
                        const textSize = this.measureElement(textOnlyElement);
                        const inputSize = this.measureElement(element);
                        // Take the width of the inner text, but keep the height of the input element.
                        return [textSize[0], inputSize[1]];
                    }
                    else if (measureContent && element instanceof HTMLTextAreaElement) {
                        const inputElement = element;
                        cleanupUnconnectedRoot(this.containerElement);
                        // Create a temporary element that will contain the input's content
                        const textOnlyElement = document.createElement("p");
                        textOnlyElement.style.cssText = unconstrainedStyleCssText;
                        // If the input is null or empty, add a no-width character to force the paragraph to take up one line height
                        // The trailing new lines are going to be ignored for measure, so we also append no-width char at the end.
                        textOnlyElement.innerText = inputElement.value ? (inputElement.value + "\u200B") : "\u200B";
                        textOnlyElement.className = elementClasses; // Note: Here we will have the uno-textBoxView class name
                        unconnectedRoot = textOnlyElement;
                        this.containerElement.appendChild(unconnectedRoot);
                        const textSize = this.measureElement(textOnlyElement);
                        // For TextAreas, take the width and height of the inner text
                        const width = Math.min(textSize[0], maxWidth);
                        const height = Math.min(textSize[1], maxHeight);
                        return [width, height];
                    }
                    else {
                        elementStyle.cssText = unconstrainedStyleCssText;
                        // As per W3C css-transform spec:
                        // https://www.w3.org/TR/css-transforms-1/#propdef-transform
                        //
                        // > For elements whose layout is governed by the CSS box model, any value other than none
                        // > for the transform property also causes the element to establish a containing block for
                        // > all descendants.Its padding box will be used to layout for all of its
                        // > absolute - position descendants, fixed - position descendants, and descendant fixed
                        // > background attachments.
                        //
                        // We use this feature to allow an measure of text without being influenced by the bounds
                        // of the viewport. We just need to temporary set both the parent width & height to a very big value.
                        parentElement = element.parentElement;
                        parentElementWidthHeight = { width: parentElement.style.width, height: parentElement.style.height };
                        parentElement.style.width = WindowManager.MAX_WIDTH;
                        parentElement.style.height = WindowManager.MAX_HEIGHT;
                        return this.measureElement(element);
                    }
                }
                finally {
                    elementStyle.cssText = originalStyleCssText;
                    if (parentElement && parentElementWidthHeight) {
                        parentElement.style.width = parentElementWidthHeight.width;
                        parentElement.style.height = parentElementWidthHeight.height;
                    }
                    cleanupUnconnectedRoot(this.containerElement);
                }
            }
            createUnconstrainedStyle(elementStyle, maxWidth, maxHeight) {
                const updatedStyles = {};
                for (let i = 0; i < elementStyle.length; i++) {
                    const key = elementStyle[i];
                    updatedStyles[key] = elementStyle.getPropertyValue(key);
                }
                if (updatedStyles.hasOwnProperty("width")) {
                    delete updatedStyles.width;
                }
                if (updatedStyles.hasOwnProperty("height")) {
                    delete updatedStyles.height;
                }
                // This is required for an unconstrained measure (otherwise the parents size is taken into account)
                updatedStyles.position = "fixed";
                updatedStyles["max-width"] = Number.isFinite(maxWidth) ? maxWidth + "px" : "none";
                updatedStyles["max-height"] = Number.isFinite(maxHeight) ? maxHeight + "px" : "none";
                let updatedStyleString = "";
                for (let key in updatedStyles) {
                    if (updatedStyles.hasOwnProperty(key)) {
                        updatedStyleString += key + ": " + updatedStyles[key] + "; ";
                    }
                }
                // This is necessary because in Safari 17 "white-space" is not selected by index (i.e. elementStyle[i])
                // This is important to implement the Wrap/NoWrap of Controls
                if (elementStyle.cssText.includes("white-space") && !updatedStyleString.includes("white-space"))
                    updatedStyleString += "white-space: " + elementStyle.whiteSpace + "; ";
                // We use a string to prevent the browser to update the element between
                // each style assignation. This way, the browser will update the element only once.
                return updatedStyleString;
            }
            scrollTo(pParams) {
                const params = WindowManagerScrollToOptionsParams.unmarshal(pParams);
                const elt = this.getView(params.HtmlId);
                const opts = ({
                    left: params.HasLeft ? params.Left : undefined,
                    top: params.HasTop ? params.Top : undefined,
                    behavior: (params.DisableAnimation ? "instant" : "smooth")
                });
                elt.scrollTo(opts);
                return true;
            }
            rawPixelsToBase64EncodeImage(dataPtr, width, height) {
                const rawCanvas = document.createElement("canvas");
                rawCanvas.width = width;
                rawCanvas.height = height;
                const ctx = rawCanvas.getContext("2d");
                const imgData = ctx.createImageData(width, height);
                const bufferSize = width * height * 4;
                for (let i = 0; i < bufferSize; i += 4) {
                    imgData.data[i + 0] = Module.HEAPU8[dataPtr + i + 2];
                    imgData.data[i + 1] = Module.HEAPU8[dataPtr + i + 1];
                    imgData.data[i + 2] = Module.HEAPU8[dataPtr + i + 0];
                    imgData.data[i + 3] = Module.HEAPU8[dataPtr + i + 3];
                }
                ctx.putImageData(imgData, 0, 0);
                return rawCanvas.toDataURL();
            }
            /**
             * Sets the provided image with a mono-chrome version of the provided url.
             * @param viewId the image to manipulate
             * @param url the source image
             * @param color the color to apply to the monochrome pixels
             */
            setImageAsMonochrome(viewId, url, color) {
                const element = this.getView(viewId);
                if (element.tagName.toUpperCase() === "IMG") {
                    const imgElement = element;
                    const img = new Image();
                    img.onload = buildMonochromeImage;
                    img.src = url;
                    function buildMonochromeImage() {
                        // create a colored version of img
                        const c = document.createElement("canvas");
                        const ctx = c.getContext("2d");
                        c.width = img.width;
                        c.height = img.height;
                        ctx.drawImage(img, 0, 0);
                        ctx.globalCompositeOperation = "source-atop";
                        ctx.fillStyle = color;
                        ctx.fillRect(0, 0, img.width, img.height);
                        ctx.globalCompositeOperation = "source-over";
                        imgElement.src = c.toDataURL();
                    }
                }
                else {
                    throw `setImageAsMonochrome: Element id ${viewId} is not an Img.`;
                }
            }
            setCornerRadius(viewId, topLeftX, topLeftY, topRightX, topRightY, bottomRightX, bottomRightY, bottomLeftX, bottomLeftY) {
                const element = this.getView(viewId);
                element.style.borderRadius = `${topLeftX}px ${topRightX}px ${bottomRightX}px ${bottomLeftX}px / ${topLeftY}px ${topRightY}px ${bottomRightY}px ${bottomLeftY}px`;
                element.style.overflow = "hidden"; // overflow: hidden is required here because the clipping can't do its job when it's non-rectangular.
            }
            focusView(elementId) {
                const element = this.getView(elementId);
                if (!(element instanceof HTMLElement)) {
                    throw `Element id ${elementId} is not focusable.`;
                }
                element.focus({ preventScroll: true });
            }
            /**
                * Set the Html content for an element.
                *
                * Those html elements won't be available as XamlElement in managed code.
                * WARNING: you should avoid mixing this and `addView` for the same element.
                */
            setHtmlContentNative(pParams) {
                const params = WindowManagerSetContentHtmlParams.unmarshal(pParams);
                this.setHtmlContentInternal(params.HtmlId, params.Html);
                return true;
            }
            setHtmlContentInternal(viewId, html) {
                this.getView(viewId).innerHTML = html;
            }
            /**
             * Gets the Client and Offset size of the specified element
             *
             * This method is used to determine the size of the scroll bars, to
             * mask the events coming from that zone.
             */
            getClientViewSizeNative(pParams, pReturn) {
                const params = WindowManagerGetClientViewSizeParams.unmarshal(pParams);
                const element = this.getView(params.HtmlId);
                const ret2 = new WindowManagerGetClientViewSizeReturn();
                ret2.ClientWidth = element.clientWidth;
                ret2.ClientHeight = element.clientHeight;
                ret2.OffsetWidth = element.offsetWidth;
                ret2.OffsetHeight = element.offsetHeight;
                ret2.marshal(pReturn);
                return true;
            }
            /**
             * Gets a dependency property value.
             *
             * Note that the casing of this method is intentionally Pascal for platform alignment.
             */
            GetDependencyPropertyValue(elementId, propertyName) {
                if (!WindowManager.getDependencyPropertyValueMethod) {
                    if (globalThis.DotnetExports !== undefined) {
                        WindowManager.getDependencyPropertyValueMethod = globalThis.DotnetExports.UnoUI.Uno.UI.Helpers.Automation.GetDependencyPropertyValue;
                    }
                    else {
                        WindowManager.getDependencyPropertyValueMethod = Module.mono_bind_static_method("[Uno.UI] Uno.UI.Helpers.Automation:GetDependencyPropertyValue");
                    }
                }
                const element = this.getView(elementId);
                const htmlId = Number(element.getAttribute("XamlHandle"));
                return WindowManager.getDependencyPropertyValueMethod(htmlId, propertyName);
            }
            /**
             * Sets a dependency property value.
             *
             * Note that the casing of this method is intentionally Pascal for platform alignment.
             */
            SetDependencyPropertyValue(elementId, propertyNameAndValue) {
                if (!WindowManager.setDependencyPropertyValueMethod) {
                    if (globalThis.DotnetExports !== undefined) {
                        WindowManager.setDependencyPropertyValueMethod = globalThis.DotnetExports.UnoUI.Uno.UI.Helpers.Automation.SetDependencyPropertyValue;
                    }
                    else {
                        throw `SetDependencyPropertyValue: Unable to find dotnet exports`;
                    }
                }
                const element = this.getView(elementId);
                const htmlId = Number(element.getAttribute("XamlHandle"));
                return WindowManager.setDependencyPropertyValueMethod(htmlId, propertyNameAndValue);
            }
            /**
                * Remove the loading indicator.
                *
                * In a future version it will also handle the splashscreen.
                */
            activate() {
                this.removeLoading();
            }
            /**
             * Creates a native element from BrowserHttpElement.
             */
            createNativeElement(elementId, unoElementId, tagname) {
                const element = document.createElement(tagname);
                element.id = elementId;
                element.unoId = unoElementId;
                // Add the html element to list of elements
                this.allActiveElementsById[this.handleToString(unoElementId)] = element;
            }
            /**
             * Dispose a native element
             */
            disposeNativeElement(unoElementId) {
                this.destroyViewInternal(unoElementId);
            }
            /**
             * Attaches a native element to a known UIElement-backed element.
             */
            attachNativeElement(ownerId, unoElementId) {
                var ownerView = this.getView(ownerId);
                var elementView = this.getView(unoElementId);
                ownerView.appendChild(elementView);
            }
            /**
             * Detaches a native element to a known UIElement-backed element.
             */
            detachNativeElement(unoElementId) {
                var view = this.getView(unoElementId);
                view.parentElement.removeChild(view);
            }
            /**
             * Registers a managed event handler
             */
            registerNativeHtmlEvent(owner, unoElementId, eventName, managedHandler) {
                const element = this.getView(unoElementId);
                const eventHandler = (event) => {
                    WindowManager.dispatchEventNativeElementMethod(owner, eventName, managedHandler, event);
                };
                // Register the handler using a string representation of the managed handler
                // the managed representation assumes that the string contains a unique id.
                this.nativeHandlersMap["" + managedHandler] = eventHandler;
                element.addEventListener(eventName, eventHandler);
            }
            /**
             * Unregisters a managed handler from its element
             */
            unregisterNativeHtmlEvent(unoElementId, eventName, managedHandler) {
                const element = this.getView(unoElementId);
                const key = "" + managedHandler;
                const eventHandler = this.nativeHandlersMap[key];
                if (eventHandler) {
                    element.removeEventListener(eventName, eventHandler);
                    delete this.nativeHandlersMap[key];
                }
            }
            init() {
                if (UnoAppManifest.displayName) {
                    document.title = UnoAppManifest.displayName;
                }
                window.addEventListener("beforeunload", () => WindowManager.dispatchSuspendingMethod());
            }
            static async initMethods() {
                await UI.ExportManager.initialize();
                if (globalThis.DotnetExports !== undefined) {
                    const exports = globalThis.DotnetExports.UnoUI;
                    WindowManager.resizeMethod = exports.Microsoft.UI.Xaml.Window.Resize;
                    WindowManager.dispatchEventMethod = exports.Microsoft.UI.Xaml.UIElement.DispatchEvent;
                    WindowManager.dispatchEventNativeElementMethod = exports.Uno.UI.NativeElementHosting.BrowserHtmlElement.DispatchEventNativeElementMethod;
                    WindowManager.focusInMethod = exports.Microsoft.UI.Xaml.Input.FocusManager.ReceiveFocusNative;
                    WindowManager.dispatchSuspendingMethod = exports.Microsoft.UI.Xaml.Application.DispatchSuspending;
                    WindowManager.keyTrackingMethod = globalThis.DotnetExports.Uno.Uno.UI.Core.KeyboardStateTracker.UpdateKeyStateNative;
                }
                else {
                    throw `WindowManager: Unable to find dotnet exports`;
                }
            }
            initDom() {
                this.containerElement = document.getElementById(this.containerElementId);
                if (!this.containerElement) {
                    // If not found, we simply create a new one.
                    this.containerElement = document.createElement("div");
                }
                document.body.addEventListener("focusin", this.onfocusin);
                document.body.appendChild(this.containerElement);
                // On WASM, if no one subscribes to key<Down|Up>, not only will the event not fire on any UIElement,
                // but the browser won't even notify us that a key was pressed/released, and this breaks KeyboardStateTracker
                // key tracking, which depends on RaiseEvent being called even if no one is subscribing. Instead, we
                // subscribe on the body and make sure to call KeyboardStateTracker ourselves here.
                document.body.addEventListener("keydown", this.onBodyKeyDown);
                document.body.addEventListener("keyup", this.onBodyKeyUp);
                window.addEventListener("resize", x => WindowManager.resize());
                window.addEventListener("contextmenu", x => {
                    if (!(x.target instanceof HTMLInputElement) ||
                        x.target.classList.contains("context-menu-disabled")) {
                        x.preventDefault();
                    }
                });
                window.addEventListener("blur", this.onWindowBlur);
            }
            removeLoading() {
                const element = document.getElementById(this.loadingElementId);
                if (element) {
                    element.parentElement.removeChild(element);
                }
                let bootstrapperLoaders = document.getElementsByClassName(WindowManager.unoPersistentLoaderClassName);
                if (bootstrapperLoaders.length > 0) {
                    let bootstrapperLoader = bootstrapperLoaders[0];
                    bootstrapperLoader.parentElement.removeChild(bootstrapperLoader);
                }
            }
            static resize() {
                WindowManager.resizeMethod(document.documentElement.clientWidth, document.documentElement.clientHeight);
            }
            onfocusin(event) {
                const newFocus = event.target;
                const handle = newFocus.getAttribute("XamlHandle");
                const htmlId = handle ? Number(handle) : -1; // newFocus may not be an Uno element
                WindowManager.focusInMethod(htmlId);
            }
            onWindowBlur() {
                // Unset managed focus when Window loses focus
                WindowManager.focusInMethod(-1);
            }
            dispatchEvent(element, eventName, eventPayload = null, onCapturePhase = false) {
                const htmlId = Number(element.getAttribute("XamlHandle"));
                // console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);
                if (!htmlId) {
                    throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
                }
                return WindowManager.dispatchEventMethod(htmlId, eventName, eventPayload || "", onCapturePhase);
            }
            getIsConnectedToRootElement(element) {
                const rootElement = this.rootElement;
                if (!rootElement) {
                    return false;
                }
                return rootElement === element || rootElement.contains(element);
            }
            handleToString(handle) {
                // Fastest conversion as of 2020-03-25 (when compared to String(handle) or handle.toString())
                return handle + "";
            }
            numberToCssColor(color) {
                return "#" + color.toString(16).padStart(8, "0");
            }
            getElementInCoordinate(x, y) {
                const element = document.elementFromPoint(x, y);
                return Number(element.getAttribute("XamlHandle"));
            }
            setCursor(cssCursor) {
                const unoBody = document.getElementById(this.containerElementId);
                if (unoBody) {
                    if (this.cursorStyleRule === undefined) {
                        const styleSheet = document.styleSheets[document.styleSheets.length - 1];
                        const ruleId = styleSheet.insertRule(".uno-buttonbase { }", styleSheet.cssRules.length);
                        this.cursorStyleRule = styleSheet.cssRules[ruleId];
                    }
                    this.cursorStyleRule.style.cursor = cssCursor !== "auto" ? cssCursor : null;
                    unoBody.style.cursor = cssCursor;
                }
                return "ok";
            }
            getNaturalImageSize(imageUrl) {
                return new Promise((resolve, reject) => {
                    const img = new Image();
                    let loadingDone = () => {
                        this.containerElement.removeChild(img);
                        resolve(`${img.width};${img.height}`);
                    };
                    let loadingError = (e) => {
                        this.containerElement.removeChild(img);
                        reject(e);
                    };
                    img.style.pointerEvents = "none";
                    img.style.opacity = "0";
                    img.onload = loadingDone;
                    img.onerror = loadingError;
                    img.src = imageUrl;
                    this.containerElement.appendChild(img);
                });
            }
            selectInputRange(elementId, start, length) {
                this.getView(elementId).setSelectionRange(start, start + length);
            }
            getIsOverflowing(elementId) {
                const element = this.getView(elementId);
                return element.clientWidth < element.scrollWidth || element.clientHeight < element.scrollHeight;
            }
            setIsFocusable(elementId, isFocusable) {
                const element = this.getView(elementId);
                element.setAttribute("tabindex", isFocusable ? "0" : "-1");
            }
            resizeWindow(width, height) {
                window.resizeTo(width, height);
            }
            moveWindow(x, y) {
                window.moveTo(x, y);
            }
            onBodyKeyDown(event) {
                WindowManager.keyTrackingMethod(event.key, true);
            }
            onBodyKeyUp(event) {
                WindowManager.keyTrackingMethod(event.key, false);
            }
            getCssColorOrUrlRef(color, paintRef) {
                if (paintRef != null) {
                    return `url(#${paintRef})`;
                }
                else if (color != null) {
                    // JSInvoke doesnt allow passing of uint, so we had to deal with int's "sign-ness" here
                    // (-1 >>> 0) is a quick hack to turn signed negative into "unsigned" positive
                    // padded to 8-digits 'RRGGBBAA', so the value doesnt get processed as 'RRGGBB' or 'RGB'.
                    return `#${(color >>> 0).toString(16).padStart(8, '0')}`;
                }
                else {
                    return '';
                }
            }
            setShapeFillStyle(elementId, color, paintRef) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.fill = this.getCssColorOrUrlRef(color, paintRef);
                }
            }
            setShapeStrokeStyle(elementId, color, paintRef) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.stroke = this.getCssColorOrUrlRef(color, paintRef);
                }
            }
            setShapeStrokeWidthStyle(elementId, strokeWidth) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.strokeWidth = `${strokeWidth}px`;
                }
            }
            setShapeStrokeDashArrayStyle(elementId, strokeDashArray) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.strokeDasharray = strokeDashArray.join(',');
                }
            }
            setShapeStylesFast1(elementId, fillColor, fillPaintRef, strokeColor, strokePaintRef) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.fill = this.getCssColorOrUrlRef(fillColor, fillPaintRef);
                    e.style.stroke = this.getCssColorOrUrlRef(strokeColor, strokePaintRef);
                }
            }
            setShapeStylesFast2(elementId, fillColor, fillPaintRef, strokeColor, strokePaintRef, strokeWidth, strokeDashArray) {
                const e = this.getView(elementId);
                if (e instanceof SVGElement) {
                    e.style.fill = this.getCssColorOrUrlRef(fillColor, fillPaintRef);
                    e.style.stroke = this.getCssColorOrUrlRef(strokeColor, strokePaintRef);
                    e.style.strokeWidth = `${strokeWidth}px`;
                    e.style.strokeDasharray = strokeDashArray.join(',');
                }
            }
            setSvgFillRule(htmlId, nonzero) {
                const e = this.getView(htmlId);
                if (e instanceof SVGPathElement) {
                    e.setAttribute('fill-rule', nonzero ? 'nonzero' : 'evenodd');
                }
            }
            setSvgEllipseAttributes(htmlId, cx, cy, rx, ry) {
                const e = this.getView(htmlId);
                if (e instanceof SVGEllipseElement) {
                    e.cx.baseVal.value = cx;
                    e.cy.baseVal.value = cy;
                    e.rx.baseVal.value = rx;
                    e.ry.baseVal.value = ry;
                }
            }
            setSvgLineAttributes(htmlId, x1, x2, y1, y2) {
                const e = this.getView(htmlId);
                if (e instanceof SVGLineElement) {
                    e.x1.baseVal.value = x1;
                    e.x2.baseVal.value = x2;
                    e.y1.baseVal.value = y1;
                    e.y2.baseVal.value = y2;
                }
            }
            setSvgPathAttributes(htmlId, nonzero, data) {
                const e = this.getView(htmlId);
                if (e instanceof SVGPathElement) {
                    e.setAttribute('fill-rule', nonzero ? 'nonzero' : 'evenodd');
                    e.setAttribute('d', data);
                }
            }
            setSvgPolyPoints(htmlId, points) {
                const e = this.getView(htmlId);
                if (e instanceof SVGPolygonElement || e instanceof SVGPolylineElement) {
                    if (points != null) {
                        const delimiters = [' ', ','];
                        // interwave to produce: x0,y0 x1,y1 ...
                        // i start at 1
                        e.setAttribute('points', points.reduce((acc, x, i) => acc + delimiters[i % delimiters.length] + x, ''));
                    }
                    else {
                        e.removeAttribute('points');
                    }
                }
            }
            setSvgRectangleAttributes(htmlId, x, y, width, height, rx, ry) {
                const e = this.getView(htmlId);
                if (e instanceof SVGRectElement) {
                    e.x.baseVal.value = x;
                    e.y.baseVal.value = y;
                    e.width.baseVal.value = width;
                    e.height.baseVal.value = height;
                    e.rx.baseVal.value = rx;
                    e.ry.baseVal.value = ry;
                }
            }
        }
        WindowManager.unoRootClassName = "uno-root-element";
        WindowManager.unoUnarrangedClassName = "uno-unarranged";
        WindowManager.unoCollapsedClassName = "uno-visibility-collapsed";
        WindowManager.unoPersistentLoaderClassName = "uno-persistent-loader";
        WindowManager.unoKeepLoaderClassName = "uno-keep-loader";
        WindowManager.MAX_WIDTH = `${Number.MAX_SAFE_INTEGER}vw`;
        WindowManager.MAX_HEIGHT = `${Number.MAX_SAFE_INTEGER}vh`;
        UI.WindowManager = WindowManager;
        if (typeof define === "function") {
            define([`./AppManifest.js`], () => {
            });
        }
        else {
            throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
        }
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// Ensure the "Uno" namespace is available globally
window.Uno = Uno;
window.Windows = Windows;
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class AsyncInteropHelper {
                static async init() {
                    if (AsyncInteropHelper.dispatchErrorMethod) {
                        return; // already initialized
                    }
                    AsyncInteropHelper.dispatchResultMethod = globalThis.DotnetExports.UnoFoundationRuntimeWebAssembly.Uno.Foundation.WebAssemblyRuntime.DispatchAsyncResult;
                    AsyncInteropHelper.dispatchErrorMethod = globalThis.DotnetExports.UnoFoundationRuntimeWebAssembly.Uno.Foundation.WebAssemblyRuntime.DispatchAsyncError;
                }
                static Invoke(handle, promiseFunction) {
                    AsyncInteropHelper.init();
                    try {
                        promiseFunction()
                            .then(str => {
                            if (typeof str == "string") {
                                AsyncInteropHelper.dispatchResultMethod(handle, str);
                            }
                            else {
                                AsyncInteropHelper.dispatchResultMethod(handle, null);
                            }
                        })
                            .catch(err => {
                            if (typeof err == "string") {
                                AsyncInteropHelper.dispatchErrorMethod(handle, err);
                            }
                            else if (err.message && err.stack) {
                                AsyncInteropHelper.dispatchErrorMethod(handle, err.message + "\n" + err.stack);
                            }
                            else {
                                AsyncInteropHelper.dispatchErrorMethod(handle, "" + err);
                            }
                        });
                    }
                    catch (err) {
                        if (typeof err == "string") {
                            AsyncInteropHelper.dispatchErrorMethod(handle, err);
                        }
                        else if (err.message && err.stack) {
                            AsyncInteropHelper.dispatchErrorMethod(handle, err.message + "\n" + err.stack);
                        }
                        else {
                            AsyncInteropHelper.dispatchErrorMethod(handle, "" + err);
                        }
                    }
                }
            }
            Interop.AsyncInteropHelper = AsyncInteropHelper;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Emscripten {
                static assert(x, message) {
                    if (!x)
                        throw new Error(message);
                }
                static warnOnce(a, msg = null) {
                    var _a;
                    if (!msg) {
                        msg = a;
                        a = false;
                    }
                    if (!a) {
                        (_a = Emscripten).msgs || (_a.msgs = {});
                        if (msg in Emscripten.msgs)
                            return;
                        Emscripten.msgs[msg] = true;
                        console.warn(msg);
                    }
                }
                // Copy of the stringToUTF8 function from the emscripten library
                static stringToUTF8Array(str, heap, outIdx, maxBytesToWrite) {
                    if (!(maxBytesToWrite > 0))
                        return 0;
                    var startIdx = outIdx;
                    var endIdx = outIdx + maxBytesToWrite - 1;
                    for (var i = 0; i < str.length; ++i) {
                        var u = str.charCodeAt(i);
                        if (u >= 55296 && u <= 57343) {
                            var u1 = str.charCodeAt(++i);
                            u = 65536 + ((u & 1023) << 10) | u1 & 1023;
                        }
                        if (u <= 127) {
                            if (outIdx >= endIdx)
                                break;
                            heap[outIdx++] = u;
                        }
                        else if (u <= 2047) {
                            if (outIdx + 1 >= endIdx)
                                break;
                            heap[outIdx++] = 192 | u >> 6;
                            heap[outIdx++] = 128 | u & 63;
                        }
                        else if (u <= 65535) {
                            if (outIdx + 2 >= endIdx)
                                break;
                            heap[outIdx++] = 224 | u >> 12;
                            heap[outIdx++] = 128 | u >> 6 & 63;
                            heap[outIdx++] = 128 | u & 63;
                        }
                        else {
                            if (outIdx + 3 >= endIdx)
                                break;
                            if (u > 1114111)
                                Emscripten.warnOnce("Invalid Unicode code point " + globalThis.Module.ptrToString(u) + " encountered when serializing a JS string to a UTF-8 string in wasm memory! (Valid unicode code points should be in range 0-0x10FFFF).");
                            heap[outIdx++] = 240 | u >> 18;
                            heap[outIdx++] = 128 | u >> 12 & 63;
                            heap[outIdx++] = 128 | u >> 6 & 63;
                            heap[outIdx++] = 128 | u & 63;
                        }
                    }
                    heap[outIdx] = 0;
                    return outIdx - startIdx;
                }
                static stringToUTF8(str, outPtr, maxBytesToWrite) {
                    Emscripten.assert(typeof maxBytesToWrite == "number", "stringToUTF8(str, outPtr, maxBytesToWrite) is missing the third parameter that specifies the length of the output buffer!");
                    return Emscripten.stringToUTF8Array(str, Module.HEAPU8, outPtr, maxBytesToWrite);
                }
            }
            Interop.Emscripten = Emscripten;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
if (globalThis.stringToUTF8 == undefined) {
    globalThis.stringToUTF8 = Uno.UI.Interop.Emscripten.stringToUTF8;
}
var Uno;
(function (Uno) {
    var Foundation;
    (function (Foundation) {
        var Interop;
        (function (Interop) {
            class ManagedObject {
                static init() {
                    var _a, _b, _c, _d, _e;
                    const exports = (_e = (_d = (_c = (_b = (_a = globalThis.DotnetExports) === null || _a === void 0 ? void 0 : _a.UnoFoundationRuntimeWebAssembly) === null || _b === void 0 ? void 0 : _b.Uno) === null || _c === void 0 ? void 0 : _c.Foundation) === null || _d === void 0 ? void 0 : _d.Interop) === null || _e === void 0 ? void 0 : _e.JSObject;
                    if (exports !== undefined) {
                        ManagedObject.dispatchMethod = exports.Dispatch;
                    }
                    else {
                        throw `ManagedObject: Unable to find dotnet exports`;
                    }
                }
                static dispatch(handle, method, parameters) {
                    if (!ManagedObject.dispatchMethod) {
                        ManagedObject.init();
                    }
                    ManagedObject.dispatchMethod(handle, method, parameters || "");
                }
            }
            Interop.ManagedObject = ManagedObject;
        })(Interop = Foundation.Interop || (Foundation.Interop = {}));
    })(Foundation = Uno.Foundation || (Uno.Foundation = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Runtime {
                static init() {
                    return "";
                }
                static InvokeJS(command) {
                    // Preseve the original emscripten marshalling semantics
                    // to always return a valid string.
                    return String(eval(command) || "");
                }
            }
            Runtime.engine = Runtime.init();
            Interop.Runtime = Runtime;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Xaml {
            }
            Interop.Xaml = Xaml;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// ReSharper disable InconsistentNaming
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Runtime;
        (function (Runtime) {
            var Skia;
            (function (Skia) {
                //TODO import PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
                var HtmlEventDispatchResult = Uno.UI.HtmlEventDispatchResult;
                let PointerDeviceType;
                (function (PointerDeviceType) {
                    PointerDeviceType[PointerDeviceType["Touch"] = 0] = "Touch";
                    PointerDeviceType[PointerDeviceType["Pen"] = 1] = "Pen";
                    PointerDeviceType[PointerDeviceType["Mouse"] = 2] = "Mouse";
                })(PointerDeviceType = Skia.PointerDeviceType || (Skia.PointerDeviceType = {}));
                let HtmlPointerEvent;
                (function (HtmlPointerEvent) {
                    HtmlPointerEvent[HtmlPointerEvent["pointerover"] = 1] = "pointerover";
                    HtmlPointerEvent[HtmlPointerEvent["pointerleave"] = 2] = "pointerleave";
                    HtmlPointerEvent[HtmlPointerEvent["pointerdown"] = 4] = "pointerdown";
                    HtmlPointerEvent[HtmlPointerEvent["pointerup"] = 8] = "pointerup";
                    HtmlPointerEvent[HtmlPointerEvent["pointercancel"] = 16] = "pointercancel";
                    // Optional pointer events
                    HtmlPointerEvent[HtmlPointerEvent["pointermove"] = 32] = "pointermove";
                    HtmlPointerEvent[HtmlPointerEvent["lostpointercapture"] = 64] = "lostpointercapture";
                    HtmlPointerEvent[HtmlPointerEvent["wheel"] = 128] = "wheel";
                })(HtmlPointerEvent = Skia.HtmlPointerEvent || (Skia.HtmlPointerEvent = {}));
                class BrowserPointerInputSource {
                    constructor(manageSource) {
                        this._bootTime = Date.now() - performance.now();
                        this._source = manageSource;
                        var userAgent = navigator.userAgent || navigator.vendor || window.opera;
                        BrowserPointerInputSource._exports.OnInitialized(manageSource, this._bootTime, userAgent);
                        this.subscribePointerEvents(); // Subscribe only after the managed initialization is done
                    }
                    static async initialize(inputSource) {
                        const module = window.Module;
                        if (BrowserPointerInputSource._exports == undefined
                            && module.getAssemblyExports !== undefined) {
                            const browserExports = (await module.getAssemblyExports("Uno.UI"));
                            BrowserPointerInputSource._exports = browserExports.Uno.UI.Runtime.BrowserPointerInputSource;
                        }
                        return new BrowserPointerInputSource(inputSource);
                    }
                    static setPointerCapture(pointerId) {
                        document.body.setPointerCapture(pointerId);
                    }
                    static releasePointerCapture(pointerId) {
                        document.body.releasePointerCapture(pointerId);
                    }
                    subscribePointerEvents() {
                        const element = document.body;
                        element.addEventListener("pointerover", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("pointerleave", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("pointerdown", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("pointerup", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("pointercancel", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("pointermove", this.onPointerEventReceived.bind(this), { capture: false });
                        element.addEventListener("wheel", this.onPointerEventReceived.bind(this), { capture: false });
                    }
                    onPointerEventReceived(evt) {
                        const event = BrowserPointerInputSource.toHtmlPointerEvent(evt.type);
                        let pointerId, pointerType, pressure;
                        let wheelDeltaX, wheelDeltaY;
                        if (evt instanceof WheelEvent) {
                            pointerId = evt.mozInputSource ? 0 : 1; // Try to match the mouse pointer ID 0 for FF, 1 for others
                            pointerType = PointerDeviceType.Mouse;
                            pressure = 0.5; // like WinUI
                            wheelDeltaX = evt.deltaX;
                            wheelDeltaY = evt.deltaY;
                            switch (evt.deltaMode) {
                                case WheelEvent.DOM_DELTA_LINE: // Actually this is supported only by FF
                                    const lineSize = BrowserPointerInputSource.wheelLineSize;
                                    wheelDeltaX *= lineSize;
                                    wheelDeltaY *= lineSize;
                                    break;
                                case WheelEvent.DOM_DELTA_PAGE:
                                    wheelDeltaX *= document.documentElement.clientWidth;
                                    wheelDeltaY *= document.documentElement.clientHeight;
                                    break;
                            }
                        }
                        else {
                            pointerId = evt.pointerId;
                            pointerType = BrowserPointerInputSource.toPointerDeviceType(evt.pointerType);
                            pressure = evt.pressure;
                            wheelDeltaX = 0;
                            wheelDeltaY = 0;
                        }
                        const result = BrowserPointerInputSource._exports.OnNativeEvent(this._source, event, //byte @event, // ONE of NativePointerEvent
                        evt.timeStamp, //double timestamp,
                        pointerType, //int deviceType, // ONE of _PointerDeviceType
                        pointerId, //double pointerId, // Warning: This is a Number in JS, and it might be negative on safari for iOS
                        evt.clientX, //double x,
                        evt.clientY, //double y,
                        evt.ctrlKey, //bool ctrl,
                        evt.shiftKey, //bool shift,
                        evt.buttons, //int buttons,
                        evt.button, //int buttonUpdate,
                        pressure, //double pressure,
                        wheelDeltaX, //double wheelDeltaX,
                        wheelDeltaY, //double wheelDeltaY,
                        evt.relatedTarget !== null //bool hasRelatedTarget)
                        );
                        // This is uesless with root pointer management
                        //if (result & HtmlEventDispatchResult.StopPropagation) {
                        //	evt.stopPropagation();
                        //}
                        if (result & HtmlEventDispatchResult.PreventDefault) {
                            evt.preventDefault();
                        }
                    }
                    static get wheelLineSize() {
                        // In web browsers, scroll might happen by pixels, line or page.
                        // But WinUI works only with pixels, so we have to convert it before send the value to the managed code.
                        // The issue is that there is no easy way get the "size of a line", instead we have to determine the CSS "line-height"
                        // defined in the browser settings. 
                        // https://stackoverflow.com/questions/20110224/what-is-the-height-of-a-line-in-a-wheel-event-deltamode-dom-delta-line
                        if (this._wheelLineSize == undefined) {
                            const el = document.createElement("div");
                            el.style.fontSize = "initial";
                            el.style.display = "none";
                            document.body.appendChild(el);
                            const fontSize = window.getComputedStyle(el).fontSize;
                            document.body.removeChild(el);
                            this._wheelLineSize = fontSize ? parseInt(fontSize) : 16; /* 16 = The current common default font size */
                            // Based on observations, even if the event reports 3 lines (the settings of windows),
                            // the browser will actually scroll of about 6 lines of text.
                            this._wheelLineSize *= 2.0;
                        }
                        return this._wheelLineSize;
                    }
                    //#endregion
                    //#region Helpers
                    static toHtmlPointerEvent(eventName) {
                        switch (eventName) {
                            case "pointerover":
                                return HtmlPointerEvent.pointerover;
                            case "pointerleave":
                                return HtmlPointerEvent.pointerleave;
                            case "pointerdown":
                                return HtmlPointerEvent.pointerdown;
                            case "pointerup":
                                return HtmlPointerEvent.pointerup;
                            case "pointercancel":
                                return HtmlPointerEvent.pointercancel;
                            case "pointermove":
                                return HtmlPointerEvent.pointermove;
                            case "wheel":
                                return HtmlPointerEvent.wheel;
                            default:
                                return undefined;
                        }
                    }
                    static toPointerDeviceType(type) {
                        switch (type) {
                            case "touch":
                                return PointerDeviceType.Touch;
                            case "pen":
                                // Note: As of 2019-11-28, once pen pressed events pressed/move/released are reported as TOUCH on Firefox
                                //		 https://bugzilla.mozilla.org/show_bug.cgi?id=1449660
                                return PointerDeviceType.Pen;
                            case "mouse":
                            default:
                                return PointerDeviceType.Mouse;
                        }
                    }
                }
                //#region WheelLineSize
                BrowserPointerInputSource._wheelLineSize = undefined;
                Skia.BrowserPointerInputSource = BrowserPointerInputSource;
            })(Skia = Runtime.Skia || (Runtime.Skia = {}));
        })(Runtime = UI.Runtime || (UI.Runtime = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Windows;
(function (Windows) {
    var ApplicationModel;
    (function (ApplicationModel) {
        var DataTransfer;
        (function (DataTransfer) {
            var DragDrop;
            (function (DragDrop) {
                var Core;
                (function (Core) {
                    class DragDropExtension {
                        constructor() {
                            // Events fired on the drop target
                            // Note: dragenter and dragover events will enable drop on the app
                            this._dropHandler = this.dispatchDropEvent.bind(this);
                            document.addEventListener("dragenter", this._dropHandler);
                            document.addEventListener("dragover", this._dropHandler);
                            document.addEventListener("dragleave", this._dropHandler); // Seems to be raised also on drop?
                            document.addEventListener("drop", this._dropHandler);
                            // Events fired on the draggable target (the source element)
                            //this._dragHandler = this.dispatchDragEvent.bind(this);
                            //document.addEventListener("dragstart", this._dragHandler);
                            //document.addEventListener("drag", this._dragHandler);
                            //document.addEventListener("dragend", this._dragHandler);
                            // #18854: Prevent the browser default selection drag preview.
                            document.addEventListener('dragstart', e => e.preventDefault());
                        }
                        static enable(pArgs) {
                            if (!DragDropExtension._dispatchDropEventMethod) {
                                if (globalThis.DotnetExports !== undefined) {
                                    DragDropExtension._dispatchDropEventMethod = globalThis.DotnetExports.UnoUI.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.OnNativeDropEvent;
                                }
                                else {
                                    throw `DragDropExtension: Unable to find dotnet exports`;
                                }
                            }
                            if (DragDropExtension._current) {
                                throw new Error("A DragDropExtension has already been enabled");
                            }
                            DragDropExtension._dispatchDragDropArgs = pArgs;
                            DragDropExtension._nextDropId = 1;
                            DragDropExtension._current = new DragDropExtension();
                        }
                        static disable(pArgs) {
                            if (DragDropExtension._dispatchDragDropArgs != pArgs) {
                                throw new Error("The current DragDropExtension does not match the provided args");
                            }
                            DragDropExtension._current.dispose();
                            DragDropExtension._current = null;
                            DragDropExtension._dispatchDragDropArgs = null;
                        }
                        dispose() {
                            // Events fired on the drop target
                            document.removeEventListener("dragenter", this._dropHandler);
                            document.removeEventListener("dragover", this._dropHandler);
                            document.removeEventListener("dragleave", this._dropHandler); // Seems to be raised also on drop?
                            document.removeEventListener("drop", this._dropHandler);
                        }
                        static registerNoOp() {
                            let notifyDisabled = (evt) => {
                                evt.dataTransfer.dropEffect = "none";
                                console.debug("Drag and Drop from external sources is disabled. See the `UnoDragDropExternalSupport` msbuild property to enable it (https://aka.platform.uno/linker-configuration)");
                                document.removeEventListener("dragenter", notifyDisabled);
                            };
                            document.addEventListener("dragenter", notifyDisabled);
                        }
                        dispatchDropEvent(evt) {
                            if (evt.type == "dragleave"
                                && evt.clientX > 0
                                && evt.clientX < document.documentElement.clientWidth
                                && evt.clientY > 0
                                && evt.clientY < document.documentElement.clientHeight) {
                                // We ignore all dragleave while if pointer is still over the window.
                                // This is to mute bubbling of drag leave when crossing boundaries of any elements on the app.
                                return;
                            }
                            if (evt.type == "dragenter") {
                                if (this._pendingDropId > 0) {
                                    // For the same reason as above, we ignore all dragenter if there is already a pending active drop
                                    return;
                                }
                                this._pendingDropId = ++DragDropExtension._nextDropId;
                            }
                            // We must keep a reference to the dataTransfer in order to be able to retrieve data items
                            this._pendingDropData = evt.dataTransfer;
                            // Prepare args
                            let args = new Core.DragDropExtensionEventArgs();
                            args.id = this._pendingDropId;
                            args.eventName = evt.type;
                            args.timestamp = evt.timeStamp;
                            args.x = evt.clientX;
                            args.y = evt.clientY;
                            args.buttons = evt.buttons;
                            args.shift = evt.shiftKey;
                            args.ctrl = evt.ctrlKey;
                            args.alt = evt.altKey;
                            if (evt.type == "dragenter") { // We use the dataItems only for enter, no needs to copy them every time!
                                const items = new Array();
                                for (let itemId = 0; itemId < evt.dataTransfer.items.length; itemId++) {
                                    const item = evt.dataTransfer.items[itemId];
                                    items.push({ id: itemId, kind: item.kind, type: item.type });
                                }
                                args.dataItems = JSON.stringify(items);
                                args.allowedOperations = evt.dataTransfer.effectAllowed;
                            }
                            else {
                                // Must be set for marshaling
                                args.dataItems = "";
                                args.allowedOperations = "";
                            }
                            args.acceptedOperation = evt.dataTransfer.dropEffect;
                            try {
                                // Raise the managed event
                                args.marshal(DragDropExtension._dispatchDragDropArgs);
                                DragDropExtension._dispatchDropEventMethod();
                                // Read response from managed code
                                args = Core.DragDropExtensionEventArgs.unmarshal(DragDropExtension._dispatchDragDropArgs);
                                evt.dataTransfer.dropEffect = (args.acceptedOperation);
                            }
                            finally {
                                // No matter if the managed code handled the event, we want to prevent thee default behavior (like opening a drop link)
                                evt.preventDefault();
                                if (evt.type == "dragleave" || evt.type == "drop") {
                                    this._pendingDropData = null;
                                    this._pendingDropId = 0;
                                }
                            }
                        }
                        static async retrieveText(itemId) {
                            const current = DragDropExtension._current;
                            const data = current === null || current === void 0 ? void 0 : current._pendingDropData;
                            if (data == null) {
                                throw new Error("No pending drag and drop data.");
                            }
                            return new Promise((resolve, reject) => {
                                const item = data.items[itemId];
                                const timeout = setTimeout(() => reject("Timeout: for security reason, you cannot access data before drop."), 15000);
                                item.getAsString(str => {
                                    clearTimeout(timeout);
                                    resolve(str);
                                });
                            });
                        }
                        static async retrieveFiles(itemIds) {
                            var _a;
                            const data = (_a = DragDropExtension._current) === null || _a === void 0 ? void 0 : _a._pendingDropData;
                            if (data == null) {
                                throw new Error("No pending drag and drop data.");
                            }
                            // Make sure to get **ALL** items content **before** going async
                            // (data.items and each instance of item will be cleared)
                            const asyncFileHandles = [];
                            for (const id of itemIds) {
                                asyncFileHandles.push(DragDropExtension.getAsFile(data.items[id]));
                            }
                            const fileHandles = [];
                            for (const asyncFile of asyncFileHandles) {
                                fileHandles.push(await asyncFile);
                            }
                            const infos = Uno.Storage.NativeStorageItem.getInfos(...fileHandles);
                            return JSON.stringify(infos);
                        }
                        static async getAsFile(item) {
                            if (item.getAsFileSystemHandle) {
                                return await item.getAsFileSystemHandle();
                            }
                            else {
                                return item.getAsFile();
                            }
                        }
                    }
                    Core.DragDropExtension = DragDropExtension;
                })(Core = DragDrop.Core || (DragDrop.Core = {}));
            })(DragDrop = DataTransfer.DragDrop || (DataTransfer.DragDrop = {}));
        })(DataTransfer = ApplicationModel.DataTransfer || (ApplicationModel.DataTransfer = {}));
    })(ApplicationModel = Windows.ApplicationModel || (Windows.ApplicationModel = {}));
})(Windows || (Windows = {}));
var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            class Application {
                static observeVisibility() {
                    if (!Application.dispatchVisibilityChange) {
                        if (globalThis.DotnetExports !== undefined) {
                            Application.dispatchVisibilityChange = globalThis.DotnetExports.UnoUI.Microsoft.UI.Xaml.Application.DispatchVisibilityChange;
                        }
                        else {
                            throw `Application: Unable to find dotnet exports`;
                        }
                    }
                    if (document.onvisibilitychange !== undefined) {
                        document.addEventListener("visibilitychange", () => {
                            Application.dispatchVisibilityChange(document.visibilityState == "visible");
                        });
                    }
                    if (window.onpagehide !== undefined) {
                        window.addEventListener("pagehide", () => {
                            Application.dispatchVisibilityChange(false);
                        });
                    }
                    if (window.onpageshow !== undefined) {
                        window.addEventListener("pageshow", () => {
                            Application.dispatchVisibilityChange(true);
                        });
                    }
                }
            }
            Xaml.Application = Application;
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Media;
            (function (Media) {
                var Animation;
                (function (Animation) {
                    class RenderingLoopAnimator {
                        static init() {
                            if (!RenderingLoopAnimator.dispatchFrame) {
                                if (globalThis.DotnetExports !== undefined) {
                                    RenderingLoopAnimator.dispatchFrame = globalThis.DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.Animation.RenderingLoopAnimator.OnFrame;
                                }
                                else {
                                    throw `Unable to find dotnet exports`;
                                }
                            }
                        }
                        static setEnabled(enabled) {
                            RenderingLoopAnimator.init();
                            RenderingLoopAnimator._isEnabled = enabled;
                            if (enabled) {
                                RenderingLoopAnimator.scheduleAnimationFrame();
                            }
                            else if (RenderingLoopAnimator._frameRequestId != null) {
                                window.cancelAnimationFrame(RenderingLoopAnimator._frameRequestId);
                                RenderingLoopAnimator._frameRequestId = null;
                            }
                        }
                        static scheduleAnimationFrame() {
                            if (RenderingLoopAnimator._frameRequestId == null) {
                                RenderingLoopAnimator._frameRequestId = window.requestAnimationFrame(RenderingLoopAnimator.onAnimationFrame);
                            }
                        }
                        static onAnimationFrame() {
                            RenderingLoopAnimator.dispatchFrame();
                            RenderingLoopAnimator._frameRequestId = null;
                            if (RenderingLoopAnimator._isEnabled) {
                                RenderingLoopAnimator.scheduleAnimationFrame();
                            }
                        }
                    }
                    RenderingLoopAnimator._isEnabled = false;
                    Animation.RenderingLoopAnimator = RenderingLoopAnimator;
                })(Animation = Media.Animation || (Media.Animation = {}));
            })(Media = Xaml.Media || (Xaml.Media = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Controls;
            (function (Controls) {
                class WebView {
                    static buildImports(assembly) {
                        if (!WebView.unoExports) {
                            window.Module.getAssemblyExports(assembly)
                                .then((e) => {
                                WebView.unoExports = e.Microsoft.UI.Xaml.Controls.NativeWebView;
                            });
                        }
                    }
                    static reload(htmlId) {
                        document.getElementById(htmlId).contentWindow.location.reload();
                    }
                    static stop(htmlId) {
                        document.getElementById(htmlId).contentWindow.stop();
                    }
                    static goBack(htmlId) {
                        document.getElementById(htmlId).contentWindow.history.back();
                    }
                    static goForward(htmlId) {
                        document.getElementById(htmlId).contentWindow.history.forward();
                    }
                    static executeScript(htmlId, script) {
                        return document.getElementById(htmlId).contentWindow.eval(script);
                    }
                    static getDocumentTitle(htmlId) {
                        return document.getElementById(htmlId).contentDocument.title;
                    }
                    static setAttribute(htmlId, name, value) {
                        document.getElementById(htmlId).setAttribute(name, value);
                    }
                    static getAttribute(htmlId, name) {
                        return document.getElementById(htmlId).getAttribute(name);
                    }
                    static navigate(htmlId, url) {
                        const iframe = document.getElementById(htmlId);
                        if (iframe) {
                            try {
                                if (iframe.contentWindow) {
                                    iframe.contentWindow.location.href = url;
                                }
                            }
                            catch (e) {
                                // Fall back to setAttribute if contentWindow access fails (cross-origin)
                                iframe.setAttribute("src", url);
                            }
                        }
                    }
                    static initializeStyling(htmlId) {
                        const iframe = document.getElementById(htmlId);
                        iframe.style.backgroundColor = "transparent";
                        iframe.style.border = "0";
                    }
                    static getPackageBase() {
                        if (WebView.cachedPackageBase !== null) {
                            return WebView.cachedPackageBase;
                        }
                        const pathsToCheck = [
                            ...Array.from(document.getElementsByTagName('script')).map(s => s.src),
                        ];
                        for (const path of pathsToCheck) {
                            const m = path === null || path === void 0 ? void 0 : path.match(/\/package_[^\/]+/);
                            if (m) {
                                const packageBase = "./" + m[0].substring(1);
                                WebView.cachedPackageBase = packageBase;
                                return packageBase;
                            }
                        }
                        WebView.cachedPackageBase = ".";
                        return ".";
                    }
                    static setupEvents(htmlId) {
                        const iframe = document.getElementById(htmlId);
                        iframe.addEventListener('load', WebView.onLoad);
                    }
                    static cleanupEvents(htmlId) {
                        const iframe = document.getElementById(htmlId);
                        iframe.removeEventListener('load', WebView.onLoad);
                    }
                    static onLoad(event) {
                        const iframe = event.currentTarget;
                        const absoluteUrl = iframe.contentWindow.location.href;
                        WebView.unoExports.DispatchLoadEvent(iframe.id, absoluteUrl);
                        try {
                            if (iframe.contentWindow && WebView.unoExports.DispatchNewWindowRequested) {
                                const unoExports = WebView.unoExports;
                                if (!iframe.contentWindow.__unoOpenOverridden) {
                                    if (!iframe.contentWindow.__unoOriginalOpen) {
                                        iframe.contentWindow.__unoOriginalOpen = iframe.contentWindow.open;
                                    }
                                    iframe.contentWindow.open = function (url, target, features) {
                                        const referer = iframe.contentWindow.location.href;
                                        const handled = unoExports.DispatchNewWindowRequested(iframe.id, url || '', referer);
                                        if (!handled) {
                                            return iframe.contentWindow.__unoOriginalOpen.call(this, url, target, features);
                                        }
                                        return null;
                                    };
                                    iframe.contentWindow.__unoOpenOverridden = true;
                                }
                                iframe.contentDocument.addEventListener('click', (e) => {
                                    const target = e.target;
                                    const link = target.closest('a[target="_blank"]');
                                    if (link) {
                                        const targetUrl = link.href;
                                        const referer = iframe.contentWindow.location.href;
                                        const handled = unoExports.DispatchNewWindowRequested(iframe.id, targetUrl, referer);
                                        if (handled) {
                                            e.preventDefault();
                                            e.stopPropagation();
                                        }
                                    }
                                });
                            }
                        }
                        catch (e) {
                            // This can fail if the iframe content is cross-origin.
                            // We log this as a warning, as it's a known browser security feature.
                            // https://developer.mozilla.org/en-US/docs/Web/Security/Same-origin_policy
                            console.warn("Uno.WebView: Could not attach NewWindowRequested handlers. This is expected if the iframe content is cross-origin.", e);
                        }
                    }
                }
                WebView.cachedPackageBase = null;
                Controls.WebView = WebView;
            })(Controls = Xaml.Controls || (Xaml.Controls = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Input;
            (function (Input) {
                class FocusVisual {
                    static attachVisual(focusVisualId, focusedElementId) {
                        FocusVisual.focusVisualId = focusVisualId;
                        FocusVisual.focusVisual = Uno.UI.WindowManager.current.getView(focusVisualId);
                        FocusVisual.focusedElement = Uno.UI.WindowManager.current.getView(focusedElementId);
                        document.addEventListener("scroll", FocusVisual.onDocumentScroll, true);
                    }
                    static detachVisual() {
                        document.removeEventListener("scroll", FocusVisual.onDocumentScroll, true);
                        FocusVisual.focusVisualId = null;
                    }
                    static onDocumentScroll() {
                        if (!FocusVisual.dispatchPositionChange) {
                            if (globalThis.DotnetExports !== undefined) {
                                FocusVisual.dispatchPositionChange = globalThis.DotnetExports.UnoUI.Uno.UI.Xaml.Controls.SystemFocusVisual.DispatchNativePositionChange;
                            }
                            else {
                                throw `FocusVisual: Unable to find dotnet exports`;
                            }
                        }
                        FocusVisual.updatePosition();
                        // Throttle managed notification while actively scrolling
                        if (FocusVisual.currentDispatchTimeout) {
                            clearTimeout(FocusVisual.currentDispatchTimeout);
                        }
                        FocusVisual.currentDispatchTimeout = setTimeout(() => FocusVisual.dispatchPositionChange(FocusVisual.focusVisualId), 100);
                    }
                    static updatePosition() {
                        const focusVisual = FocusVisual.focusVisual;
                        const focusedElement = FocusVisual.focusedElement;
                        const boundingRect = focusedElement.getBoundingClientRect();
                        const centerX = boundingRect.x + boundingRect.width / 2;
                        const centerY = boundingRect.y + boundingRect.height / 2;
                        focusVisual.style.setProperty("left", boundingRect.x + "px");
                        focusVisual.style.setProperty("top", boundingRect.y + "px");
                    }
                }
                Input.FocusVisual = FocusVisual;
            })(Input = Xaml.Input || (Xaml.Input = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
var Microsoft;
(function (Microsoft) {
    var UI;
    (function (UI) {
        var Xaml;
        (function (Xaml) {
            var Media;
            (function (Media) {
                class FontFamily {
                    static async loadFont(fontFamilyName, fontSource) {
                        try {
                            // Launch the loading of the font
                            const font = new FontFace(fontFamilyName, `url(${fontSource})`);
                            // Wait for the font to be loaded
                            await font.load();
                            // Make it available to document
                            document.fonts.add(font);
                            await FontFamily.forceFontUsage(fontFamilyName);
                        }
                        catch (e) {
                            console.debug(`Font failed to load ${e}`);
                            FontFamily.notifyFontLoadFailed(fontFamilyName);
                        }
                    }
                    static async forceFontUsage(fontFamilyName) {
                        // Force the browser to use it
                        const dummyHiddenElement = document.createElement("p");
                        dummyHiddenElement.style.fontFamily = fontFamilyName;
                        dummyHiddenElement.style.opacity = "0";
                        dummyHiddenElement.style.pointerEvents = "none";
                        dummyHiddenElement.innerText = fontFamilyName;
                        document.body.appendChild(dummyHiddenElement);
                        // Yield an animation frame
                        await new Promise((ok, err) => requestAnimationFrame(() => ok(null)));
                        // Remove dummy element
                        document.body.removeChild(dummyHiddenElement);
                        // Notify font as loaded to application
                        FontFamily.notifyFontLoaded(fontFamilyName);
                    }
                    static notifyFontLoaded(fontFamilyName) {
                        if (!FontFamily.managedNotifyFontLoaded) {
                            if (globalThis.DotnetExports !== undefined) {
                                FontFamily.managedNotifyFontLoaded = globalThis.DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.FontFamilyLoader.NotifyFontLoaded;
                            }
                            else {
                                throw `FontFamily: Unable to find dotnet exports`;
                            }
                        }
                        FontFamily.managedNotifyFontLoaded(fontFamilyName);
                    }
                    static notifyFontLoadFailed(fontFamilyName) {
                        if (!FontFamily.managedNotifyFontLoadFailed) {
                            if (globalThis.DotnetExports !== undefined) {
                                FontFamily.managedNotifyFontLoadFailed = globalThis.DotnetExports.UnoUI.Microsoft.UI.Xaml.Media.FontFamilyLoader.NotifyFontLoadFailed;
                            }
                            else {
                                throw `FontFamily: Unable to find dotnet exports`;
                            }
                        }
                        FontFamily.managedNotifyFontLoadFailed(fontFamilyName);
                    }
                }
                Media.FontFamily = FontFamily;
            })(Media = Xaml.Media || (Xaml.Media = {}));
        })(Xaml = UI.Xaml || (UI.Xaml = {}));
    })(UI = Microsoft.UI || (Microsoft.UI = {}));
})(Microsoft || (Microsoft = {}));
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerAddViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerAddViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        {
            ret.Index = Number(Module.getValue(pData + 8, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxParams {
    static unmarshal(pData) {
        const ret = new WindowManagerGetBBoxParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.X, "double");
        Module.setValue(pData + 8, this.Y, "double");
        Module.setValue(pData + 16, this.Width, "double");
        Module.setValue(pData + 24, this.Height, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetClientViewSizeParams {
    static unmarshal(pData) {
        const ret = new WindowManagerGetClientViewSizeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetClientViewSizeReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.OffsetWidth, "double");
        Module.setValue(pData + 8, this.OffsetHeight, "double");
        Module.setValue(pData + 16, this.ClientWidth, "double");
        Module.setValue(pData + 24, this.ClientHeight, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.DesiredWidth, "double");
        Module.setValue(pData + 8, this.DesiredHeight, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRegisterEventOnViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.EventName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.EventName = null;
            }
        }
        {
            ret.OnCapturePhase = Boolean(Module.getValue(pData + 8, "i32"));
        }
        {
            ret.EventExtractorId = Number(Module.getValue(pData + 12, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveAttributeParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRemoveAttributeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveViewParams {
    static unmarshal(pData) {
        const ret = new WindowManagerRemoveViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetElementBackgroundParams {
    static unmarshal(pData) {
        const ret = new WindowManagerResetElementBackgroundParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerScrollToOptionsParams {
    static unmarshal(pData) {
        const ret = new WindowManagerScrollToOptionsParams();
        {
            ret.Left = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Top = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.HasLeft = Boolean(Module.getValue(pData + 16, "i32"));
        }
        {
            ret.HasTop = Boolean(Module.getValue(pData + 20, "i32"));
        }
        {
            ret.DisableAnimation = Boolean(Module.getValue(pData + 24, "i32"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 28, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetContentHtmlParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetContentHtmlParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Html = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Html = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementBackgroundColorParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetElementBackgroundColorParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Color = Module.HEAPU32[(pData + 4) >> 2];
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementBackgroundGradientParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetElementBackgroundGradientParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.CssGradient = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.CssGradient = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementColorParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetElementColorParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Color = Module.HEAPU32[(pData + 4) >> 2];
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementFillParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetElementFillParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Color = Module.HEAPU32[(pData + 4) >> 2];
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetNameParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetNameParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSelectionHighlightParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetSelectionHighlightParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.BackgroundColor = Module.HEAPU32[(pData + 4) >> 2];
        }
        {
            ret.ForegroundColor = Module.HEAPU32[(pData + 8) >> 2];
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStyleDoubleParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetStyleDoubleParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        {
            ret.Value = Number(Module.getValue(pData + 8, "double"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSvgElementRectParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetSvgElementRectParams();
        {
            ret.X = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Y = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.Width = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.Height = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 32, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetXUidParams {
    static unmarshal(pData) {
        const ret = new WindowManagerSetXUidParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            const ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Uid = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Uid = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
var Windows;
(function (Windows) {
    var ApplicationModel;
    (function (ApplicationModel) {
        var DataTransfer;
        (function (DataTransfer) {
            var DragDrop;
            (function (DragDrop) {
                var Core;
                (function (Core) {
                    class DragDropExtensionEventArgs {
                        static unmarshal(pData) {
                            const ret = new DragDropExtensionEventArgs();
                            {
                                const ptr = Module.getValue(pData + 0, "*");
                                if (ptr !== 0) {
                                    ret.eventName = String(Module.UTF8ToString(ptr));
                                }
                                else {
                                    ret.eventName = null;
                                }
                            }
                            {
                                const ptr = Module.getValue(pData + 4, "*");
                                if (ptr !== 0) {
                                    ret.allowedOperations = String(Module.UTF8ToString(ptr));
                                }
                                else {
                                    ret.allowedOperations = null;
                                }
                            }
                            {
                                const ptr = Module.getValue(pData + 8, "*");
                                if (ptr !== 0) {
                                    ret.acceptedOperation = String(Module.UTF8ToString(ptr));
                                }
                                else {
                                    ret.acceptedOperation = null;
                                }
                            }
                            {
                                const ptr = Module.getValue(pData + 12, "*");
                                if (ptr !== 0) {
                                    ret.dataItems = String(Module.UTF8ToString(ptr));
                                }
                                else {
                                    ret.dataItems = null;
                                }
                            }
                            {
                                ret.timestamp = Number(Module.getValue(pData + 16, "double"));
                            }
                            {
                                ret.x = Number(Module.getValue(pData + 24, "double"));
                            }
                            {
                                ret.y = Number(Module.getValue(pData + 32, "double"));
                            }
                            {
                                ret.id = Number(Module.getValue(pData + 40, "i32"));
                            }
                            {
                                ret.buttons = Number(Module.getValue(pData + 44, "i32"));
                            }
                            {
                                ret.shift = Boolean(Module.getValue(pData + 48, "i32"));
                            }
                            {
                                ret.ctrl = Boolean(Module.getValue(pData + 52, "i32"));
                            }
                            {
                                ret.alt = Boolean(Module.getValue(pData + 56, "i32"));
                            }
                            return ret;
                        }
                        marshal(pData) {
                            {
                                const stringLength = lengthBytesUTF8(this.eventName);
                                const pString = Module._malloc(stringLength + 1);
                                stringToUTF8(this.eventName, pString, stringLength + 1);
                                Module.setValue(pData + 0, pString, "*");
                            }
                            {
                                const stringLength = lengthBytesUTF8(this.allowedOperations);
                                const pString = Module._malloc(stringLength + 1);
                                stringToUTF8(this.allowedOperations, pString, stringLength + 1);
                                Module.setValue(pData + 4, pString, "*");
                            }
                            {
                                const stringLength = lengthBytesUTF8(this.acceptedOperation);
                                const pString = Module._malloc(stringLength + 1);
                                stringToUTF8(this.acceptedOperation, pString, stringLength + 1);
                                Module.setValue(pData + 8, pString, "*");
                            }
                            {
                                const stringLength = lengthBytesUTF8(this.dataItems);
                                const pString = Module._malloc(stringLength + 1);
                                stringToUTF8(this.dataItems, pString, stringLength + 1);
                                Module.setValue(pData + 12, pString, "*");
                            }
                            Module.setValue(pData + 16, this.timestamp, "double");
                            Module.setValue(pData + 24, this.x, "double");
                            Module.setValue(pData + 32, this.y, "double");
                            Module.setValue(pData + 40, this.id, "i32");
                            Module.setValue(pData + 44, this.buttons, "i32");
                            Module.setValue(pData + 48, this.shift, "i32");
                            Module.setValue(pData + 52, this.ctrl, "i32");
                            Module.setValue(pData + 56, this.alt, "i32");
                        }
                    }
                    Core.DragDropExtensionEventArgs = DragDropExtensionEventArgs;
                })(Core = DragDrop.Core || (DragDrop.Core = {}));
            })(DragDrop = DataTransfer.DragDrop || (DataTransfer.DragDrop = {}));
        })(DataTransfer = ApplicationModel.DataTransfer || (ApplicationModel.DataTransfer = {}));
    })(ApplicationModel = Windows.ApplicationModel || (Windows.ApplicationModel = {}));
})(Windows || (Windows = {}));
