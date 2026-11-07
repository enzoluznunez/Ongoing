using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class Strings
{
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        ["option.run"] = new[] { "Run", "Ejecutar" },
        ["option.onHold"] = new[] { "On hold", "En espera" },
        ["assistant.speed"] = new[] { "Assistant Speed", "Velocidad del asistente" },
        ["tool.steps"] = new[] { "How to use it", "Cómo usarla" },
        ["tool.Move.step1"] = new[] { "Reach toward the chart, or point at it from a distance.", "Acerca la mano al gráfico o apúntalo desde lejos." },
        ["tool.Move.step2"] = new[] { "Pinch with one hand.", "Pellizca con una mano." },
        ["tool.Move.step3"] = new[] { "Carry it where you want it, then let go.", "Llévalo adonde quieras y suéltalo." },
        ["tool.Rotate.step1"] = new[] { "Reach toward the chart, or point at it from a distance, with both hands.", "Acerca ambas manos al gráfico o apúntalo desde lejos." },
        ["tool.Rotate.step2"] = new[] { "Pinch with both hands.", "Pellizca con ambas manos." },
        ["tool.Rotate.step3"] = new[] { "Twist your hands around each other, then let go.", "Gira las manos una alrededor de la otra y suéltalo." },
        ["tool.Scale.step1"] = new[] { "Reach toward the chart, or point at it from a distance, with both hands.", "Acerca ambas manos al gráfico o apúntalo desde lejos." },
        ["tool.Scale.step2"] = new[] { "Pinch with both hands.", "Pellizca con ambas manos." },
        ["tool.Scale.step3"] = new[] { "Move your hands apart to enlarge it or together to shrink it, then let go.", "Separa las manos para agrandarlo o júntalas para achicarlo y suéltalo." },
        ["setting.Backdrop"] = new[] { "Backdrop", "Fondo" },
        ["setting.BackdropGridlines"] = new[] { "Backdrop Gridlines", "Cuadrícula del fondo" },
        ["setting.TickerLabels"] = new[] { "Ticker Labels", "Etiquetas de marcas" },
        ["setting.AxisLabels"] = new[] { "Axis Labels", "Títulos de ejes" },
        ["setting.ChartKey"] = new[] { "Chart Key", "Leyenda del gráfico" },
        ["setting.ChartDescription"] = new[] { "Chart Description", "Descripción del gráfico" },
        ["setting.Animations.desc"] = new[] { "Off makes everything land in place at once instead of moving there.", "Desactivado hace que todo aparezca en su lugar al instante." },
        ["setting.SoundEffects.desc"] = new[] { "The click a press makes.", "El clic que suena al presionar." },
        ["setting.Handedness.desc"] = new[] { "Which hand the wrist buttons float over.", "Sobre qué mano flotan los botones de muñeca." },
        ["setting.TextSize.desc"] = new[] { "Panel and button text. The panels keep their size.", "El texto de paneles y botones. Los paneles mantienen su tamaño." },
        ["setting.ColorMode.desc"] = new[] { "Palettes for red-green and blue-yellow color blindness. The key shows the open chart's colors.", "Paletas para daltonismo rojo-verde y azul-amarillo. La leyenda muestra los colores del gráfico abierto." },
        ["setting.Language.desc"] = new[] { "The app's text and the assistant's language.", "El texto de la aplicación y el idioma del asistente." },
        ["setting.ExplanationLevel.desc"] = new[] { "How simply or technically the assistant explains.", "Con qué sencillez o tecnicismo explica el asistente." },
        ["setting.Backdrop.desc"] = new[] { "A floor and two back walls behind every chart, which swap sides as you walk around it.", "Un piso y dos paredes detrás de cada gráfico, que cambian de lado al rodearlo." },
        ["setting.BackdropGridlines.desc"] = new[] { "Lines on the floor and walls that help you read values.", "Líneas en el piso y las paredes que ayudan a leer los valores." },
        ["setting.TickerLabels.desc"] = new[] { "The values and names along each axis, and names on tiles and people.", "Los valores y nombres en cada eje, y los nombres en mosaicos y personas." },
        ["setting.AxisLabels.desc"] = new[] { "The title of each axis.", "El título de cada eje." },
        ["setting.ChartKey.desc"] = new[] { "What each color means, on a card beside the chart.", "Qué significa cada color, en una tarjeta junto al gráfico." },
        ["setting.ChartDescription.desc"] = new[] { "The chart's title and what it shows, on a card beside the chart.", "El título del gráfico y lo que muestra, en una tarjeta junto a él." },
        ["setting.ChartStyle"] = new[] { "Chart Style", "Estilo del gráfico" },
        ["setting.ChartStyle.desc"] = new[] { "Solid is one flat color per part; Shaded gives surfaces soft light and dark sides.", "Sólido usa un color plano; Sombreado da a las superficies lados claros y oscuros." },
        ["setting.ChartOpacity"] = new[] { "Chart Opacity", "Opacidad del gráfico" },
        ["setting.ChartOpacity.desc"] = new[] { "How see-through the chart's marks are.", "Cuánto se transparentan las marcas del gráfico." },
        ["setting.BackdropOpacity"] = new[] { "Backdrop Opacity", "Opacidad del fondo" },
        ["setting.BackdropOpacity.desc"] = new[] { "How strongly the floor and walls show over your room.", "Con qué fuerza se ven el piso y las paredes sobre tu habitación." },
        ["setting.ChartLabelSize"] = new[] { "Chart Label Size", "Tamaño de etiquetas" },
        ["setting.ChartLabelSize.desc"] = new[] { "The size of labels on the chart. Text Size sets panel text.", "El tamaño de las etiquetas del gráfico. Tamaño del texto ajusta los paneles." },
        ["setting.ChartTheme"] = new[] { "Chart Theme", "Tema del gráfico" },
        ["setting.ChartTheme.desc"] = new[] { "Dark uses white labels; Light uses dark labels on a pale backdrop for bright rooms.", "Oscuro usa etiquetas blancas; Claro usa etiquetas oscuras sobre un fondo pálido para habitaciones luminosas." },
        ["setting.DefaultChartSize"] = new[] { "Default Chart Size", "Tamaño inicial" },
        ["setting.DefaultChartSize.desc"] = new[] { "The size a chart opens at.", "El tamaño con el que se abre un gráfico." },
        ["setting.MotionSpeed"] = new[] { "Motion Speed", "Velocidad de movimiento" },
        ["setting.MotionSpeed.desc"] = new[] { "How fast charts rise, sink and glide. Animations turns this off.", "Qué tan rápido suben, bajan y se deslizan los gráficos. Animaciones lo desactiva." },
        ["setting.ChartHeight"] = new[] { "Chart Height", "Altura del gráfico" },
        ["setting.ChartHeight.desc"] = new[] { "How high charts open. Low suits sitting.", "A qué altura se abren los gráficos. Baja es cómoda sentado." },
        ["setting.ChartDistance"] = new[] { "Open Distance", "Distancia de apertura" },
        ["setting.ChartDistance.desc"] = new[] { "How far ahead charts and panels open.", "A qué distancia se abren gráficos y paneles." },
        ["setting.PanelContrast"] = new[] { "Panel Contrast", "Contraste de paneles" },
        ["setting.PanelContrast.desc"] = new[] { "High makes panel text bolder and greyed-out rows easier to read.", "Alto hace el texto más grueso y las filas atenuadas más legibles." },
        ["setting.SoundVolume"] = new[] { "Sound Volume", "Volumen de sonidos" },
        ["setting.SoundVolume.desc"] = new[] { "How loud the click of a press is.", "Qué tan fuerte suena el clic al presionar." },
        ["setting.Captions"] = new[] { "Captions", "Subtítulos" },
        ["setting.Captions.desc"] = new[] { "Shows what you and the assistant say in a bar low in your view.", "Muestra lo que dicen tú y el asistente en una barra baja en tu vista." },
        ["setting.AssistantSpeed"] = new[] { "Assistant Speed", "Velocidad del asistente" },
        ["setting.AssistantSpeed.desc"] = new[] { "How fast the assistant's changes play out.", "Qué tan rápido se ven los cambios del asistente." },
        ["setting.AssistantVoice"] = new[] { "Assistant Voice", "Voz del asistente" },
        ["setting.AssistantVoice.desc"] = new[] { "The assistant's voice. Changing it starts a new conversation.", "La voz del asistente. Cambiarla inicia una conversación nueva." },
        ["setting.AssistantVolume"] = new[] { "Assistant Volume", "Volumen del asistente" },
        ["setting.AssistantVolume.desc"] = new[] { "How loud the assistant speaks.", "Qué tan fuerte habla el asistente." },
        ["setting.WebSearch"] = new[] { "Web Search", "Búsqueda web" },
        ["setting.WebSearch.desc"] = new[] { "Lets the assistant search Google for questions the app can't answer. Changing it starts a new conversation.", "Permite que el asistente busque en Google lo que la aplicación no sabe. Cambiarlo inicia una conversación nueva." },
        ["setting.NumberFormat"] = new[] { "Number Format", "Formato de números" },
        ["setting.NumberFormat.desc"] = new[] { "Short shows 12.5k; Full shows 12,500.", "Corto muestra 12.5k; Completo muestra 12,500." },
        ["setting.NetworkLabels"] = new[] { "Network Labels", "Etiquetas de redes" },
        ["setting.NetworkLabels.desc"] = new[] { "Which people are named on network charts.", "Qué personas se nombran en los gráficos de redes." },
        ["setting.StartupChart"] = new[] { "Startup Chart", "Gráfico inicial" },
        ["setting.StartupChart.desc"] = new[] { "The chart that opens when the app starts.", "El gráfico que se abre al iniciar la aplicación." },
        ["setting.Reset"] = new[] { "Reset All Settings", "Restablecer ajustes" },
        ["setting.Reset.desc"] = new[] { "Puts every setting back to its default.", "Devuelve todos los ajustes a sus valores predeterminados." },
        ["setting.Recenter"] = new[] { "Recenter", "Recentrar" },
        ["setting.Recenter.desc"] = new[] { "Brings the layout back in front of you.", "Trae la disposición de vuelta frente a ti." },
        ["option.Solid"] = new[] { "Solid", "Sólido" },
        ["option.Shaded"] = new[] { "Shaded", "Sombreado" },
        ["option.P25"] = new[] { "25%", "25%" },
        ["option.P50"] = new[] { "50%", "50%" },
        ["option.P75"] = new[] { "75%", "75%" },
        ["option.P100"] = new[] { "100%", "100%" },
        ["option.Low"] = new[] { "Low", "Baja" },
        ["option.High"] = new[] { "High", "Alta" },
        ["option.Dark"] = new[] { "Dark", "Oscuro" },
        ["option.Light"] = new[] { "Light", "Claro" },
        ["option.Near"] = new[] { "Near", "Cerca" },
        ["option.Far"] = new[] { "Far", "Lejos" },
        ["option.Normal"] = new[] { "Normal", "Normal" },
        ["option.Slow"] = new[] { "Slow", "Lenta" },
        ["option.Fast"] = new[] { "Fast", "Rápida" },
        ["option.Short"] = new[] { "Short", "Corto" },
        ["option.Full"] = new[] { "Full", "Completo" },
        ["option.Hubs"] = new[] { "Hubs only", "Solo centrales" },
        ["option.Everyone"] = new[] { "Everyone", "Todos" },
        ["option.CatalogDefault"] = new[] { "Default", "Predeterminado" },
        ["speed.half"] = new[] { "0.5 m/s", "0.5 m/s" },
        ["speed.one"] = new[] { "1 m/s", "1 m/s" },
        ["section.Interface"] = new[] { "Interface", "Interfaz" },
        ["section.ChartParts"] = new[] { "Chart Parts", "Partes del gráfico" },
        ["section.ChartLook"] = new[] { "Chart Look", "Aspecto del gráfico" },
        ["section.Comfort"] = new[] { "Comfort and Accessibility", "Comodidad y accesibilidad" },
        ["section.Assistant"] = new[] { "Voice Assistant", "Asistente de voz" },
        ["section.Data"] = new[] { "Data", "Datos" },
        ["section.App"] = new[] { "App", "Aplicación" },
        ["option.P125"] = new[] { "125%", "125%" },
        ["panel.tools"] = new[] { "Tools", "Herramientas" },
        ["panel.settings"] = new[] { "Settings", "Ajustes" },
        ["wrist.tools"] = new[] { "Tool Panel", "Panel de herramientas" },
        ["wrist.settings"] = new[] { "Settings", "Ajustes" },
        ["undo"] = new[] { "Undo", "Deshacer" },
        ["undo.all"] = new[] { "Undo All", "Deshacer todo" },

        ["tool.Move"] = new[] { "Move", "Mover" },
        ["tool.Rotate"] = new[] { "Rotate", "Rotar" },
        ["tool.Scale"] = new[] { "Scale", "Escalar" },
        ["tool.Profile"] = new[] { "Profile", "Perfil" },
        ["tool.Filter"] = new[] { "Filter", "Filtrar" },
        ["tool.Sort"] = new[] { "Sort", "Ordenar" },
        ["tool.Move.desc"] = new[] { "Moves the chart anywhere in the room.", "Mueve el gráfico a cualquier lugar de la sala." },
        ["tool.Rotate.desc"] = new[] { "Turns the chart to face any direction.", "Gira el gráfico hacia cualquier dirección." },
        ["tool.Scale.desc"] = new[] { "Makes the chart larger or smaller.", "Agranda o achica el gráfico." },
        ["tool.Profile.desc"] = new[] { "Raises rows or columns above the sheet.", "Eleva filas o columnas sobre la hoja." },
        ["tool.Sort.desc"] = new[] { "Reorders the rows and columns of the sheet.", "Reordena las filas y columnas de la hoja." },
        ["tool.Filter.desc"] = new[] { "Hides or shows rows and columns on the sheet.", "Oculta o muestra filas y columnas de la hoja." },

        ["assistant"] = new[] { "Assistant", "Asistente" },
        ["assistant.desc"] = new[] { "Carries out your spoken requests using the same tools.", "Hace lo que le pides en voz alta con las mismas herramientas." },
        ["assistant.connecting"] = new[] { "Connecting", "Conectando" },
        ["speed.instant"] = new[] { "Instant", "Instantáneo" },

        ["setting.Animations"] = new[] { "Animations", "Animaciones" },
        ["setting.SoundEffects"] = new[] { "Sound Effects", "Efectos de sonido" },
        ["setting.Handedness"] = new[] { "Handedness", "Mano dominante" },
        ["setting.TextSize"] = new[] { "Text Size", "Tamaño del texto" },
        ["setting.ColorMode"] = new[] { "Color Mode", "Modo de color" },
        ["setting.Language"] = new[] { "Language Mode", "Modo de idioma" },
        ["setting.ExplanationLevel"] = new[] { "Voice Assistant's Explanation Level", "Nivel de explicación del asistente de voz" },

        ["option.on"] = new[] { "On", "Activado" },
        ["option.off"] = new[] { "Off", "Desactivado" },
        ["option.Left"] = new[] { "Left", "Izquierda" },
        ["option.Right"] = new[] { "Right", "Derecha" },
        ["option.Small"] = new[] { "Small", "Pequeño" },
        ["option.Medium"] = new[] { "Medium", "Mediano" },
        ["option.Large"] = new[] { "Large", "Grande" },
        ["option.Standard"] = new[] { "Standard", "Estándar" },
        ["option.RedGreen"] = new[] { "Red-Green", "Rojo-verde" },
        ["option.BlueYellow"] = new[] { "Blue-Yellow", "Azul-amarillo" },
        ["option.Simple"] = new[] { "Simple", "Sencillo" },
        ["option.Expert"] = new[] { "Expert", "Experto" },
        ["option.English"] = new[] { "English", "English" },
        ["option.Spanish"] = new[] { "Español", "Español" },

        ["colorkey.desc"] = new[] { "Shows what each color stands for on the chart.", "Muestra qué representa cada color en el gráfico." },
        ["colorkey.other"] = new[] { "Everything else", "Todo lo demás" },
        ["colorkey.none"] = new[] { "This chart has no colors to explain.", "Este gráfico no tiene colores que explicar." },

        ["loading"] = new[] { "Loading", "Cargando" },
        ["loading.listing"] = new[] { "Listing the charts", "Enumerando los gráficos" },
        ["loading.reading"] = new[] { "Building the chart", "Construyendo el gráfico" },
        ["loading.graphics"] = new[] { "Preparing the graphics", "Preparando los gráficos" },

        ["notice.assistantOff"] = new[] { "Assistant Off", "Asistente apagado" },
        ["notice.micDenied"] = new[] { "Microphone access is denied, so the assistant cannot listen.", "No hay acceso al micrófono, así que el asistente no puede escuchar." },
        ["notice.disconnected"] = new[] { "The assistant could not stay connected and turned itself off.", "El asistente no pudo mantenerse conectado y se apagó." },
        ["notice.nearlyFull"] = new[] { "Assistant Nearly Full", "Asistente casi lleno" },
        ["notice.nearlyFull.body"] = new[] { "The assistant is close to the limit of what it can hold in mind. It will reset itself shortly and start over from a blank slate.", "El asistente está cerca del límite de lo que puede recordar. Pronto se reiniciará y empezará de cero." },
        ["notice.full"] = new[] { "Assistant Full", "Asistente lleno" },
        ["notice.full.open"] = new[] { "The assistant ran out of room to hold the conversation and stopped. Select the assistant again to start over.", "El asistente se quedó sin espacio para la conversación y se detuvo. Selecciona el asistente de nuevo para empezar otra vez." },
        ["notice.full.closed"] = new[] { "The assistant ran out of room to hold the conversation and stopped. To start over, open the Tool Panel and select the assistant.", "El asistente se quedó sin espacio para la conversación y se detuvo. Para empezar otra vez, abre el panel de herramientas y selecciona el asistente." },
        ["notice.stopped"] = new[] { "Assistant Stopped", "Asistente detenido" },
        ["notice.stopped.one"] = new[] { "One change was already made. Undo takes it back.", "Ya se hizo un cambio. Deshacer lo revierte." },
        ["notice.stopped.many"] = new[] { "{0} changes were already made. Undo takes them back in one step.", "Ya se hicieron {0} cambios. Deshacer los revierte en un solo paso." },
        ["notice.noCharts"] = new[] { "No Charts", "Sin gráficos" },
        ["notice.noCharts.body"] = new[] { "There is no {0} in StreamingAssets to list the charts from.", "No hay ningún {0} en StreamingAssets para enumerar los gráficos." },
        ["notice.chartUnavailable"] = new[] { "Chart Unavailable", "Gráfico no disponible" },
    };

    private static int Column => Settings.Language == AppLanguage.Spanish ? 1 : 0;

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out string[] row))
        {
            Debug.LogWarning($"[Strings] No text for '{key}'.");
            return key;
        }
        return row[Column];
    }

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    public static event Action Changed
    {
        add { Listen(); _changed += value; }
        remove { _changed -= value; }
    }
    private static Action _changed;

    private static readonly List<(TMP_Text label, string key)> _bound = new List<(TMP_Text, string)>();
    private static bool _listening;

    public static void Bind(TMP_Text label, string key)
    {
        if (label == null) return;
        Listen();
        for (int i = 0; i < _bound.Count; i++)
        {
            if (_bound[i].label != label) continue;
            _bound[i] = (label, key);
            label.text = Get(key);
            return;
        }
        _bound.Add((label, key));
        label.text = Get(key);
    }

    private static void Listen()
    {
        if (_listening) return;
        _listening = true;
        Settings.Changed += OnSettingChanged;
    }

    private static void OnSettingChanged(Setting which)
    {
        if (which != Setting.Language) return;
        for (int i = _bound.Count - 1; i >= 0; i--)
        {
            if (_bound[i].label == null) { _bound.RemoveAt(i); continue; }
            _bound[i].label.text = Get(_bound[i].key);
        }
        _changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _bound.Clear();
        _listening = false;
        _changed = null;
    }
}
