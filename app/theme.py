"""
Theme dùng chung cho toàn bộ dashboard AIsle. Gọi apply_theme() ở ĐẦU mỗi
file trong app/pages/*.py, ngay sau st.set_page_config.

Phong cách: Stardew Valley — Wooden Pixel Art (lấy cảm hứng từ bản thiết kế
Google Stitch "Retail Simulation Dashboard v2").
"""
import streamlit as st

CSS = """
<style>
/* ───────── FONTS ───────── */
@import url('https://fonts.googleapis.com/css2?family=Pixelify+Sans:wght@400;500;600;700&family=VT323&family=IBM+Plex+Mono:wght@400;500;600&family=Inter:wght@400;500;600&display=swap');

/* ───────── COLOR TOKENS ───────── */
:root{
    /* Gỗ & Nền */
    --wood-darkest: #1a0e06;
    --wood-dark:    #2e1509;
    --wood-mid:     #5c2e14;
    --wood-light:   #8d4a23;
    --wood-lighter: #a86732;
    --wood-panel:   #3a1c0d;
    --wood-surface: #4a2512;

    /* Chữ & Nhấn */
    --text-cream:   #f5e6c8;
    --text-gold:    #ffca58;
    --text-dim:     #b8946a;
    --amber-gold:   #ffc24b;
    --green-pixel:  #5dba4f;
    --red-pixel:    #e05252;
    --blue-pixel:   #5fa8d3;
    --purple-pixel: #a87bca;

    /* Viền & Bevel */
    --bevel-light:  #c8844a;
    --bevel-dark:   #1f0c03;
    --border-wood:  #6b3519;
    --groove:       #241008;
}

/* ───────── GLOBAL RESET ───────── */
html, body, [data-testid="stAppViewContainer"], [data-testid="stHeader"],
[data-testid="stBottomBlockContainer"] {
    background-color: var(--wood-darkest) !important;
}

[data-testid="stAppViewContainer"]{
    background-image:
        repeating-linear-gradient(
            90deg,
            transparent,
            transparent 62px,
            rgba(90,50,20,.12) 62px,
            rgba(90,50,20,.12) 64px
        ),
        repeating-linear-gradient(
            0deg,
            transparent,
            transparent 62px,
            rgba(90,50,20,.08) 62px,
            rgba(90,50,20,.08) 64px
        ),
        linear-gradient(180deg, #1a0e06 0%, #2e1509 100%);
    background-size: auto;
}

[data-testid="stHeader"]{ background:transparent !important; }
footer{ visibility:hidden; }

/* ───────── SIDEBAR — Bảng gỗ bên trái ───────── */
[data-testid="stSidebar"]{
    background: linear-gradient(180deg, #2e1509 0%, #3a1c0d 50%, #2e1509 100%) !important;
    border-right: 4px solid var(--border-wood) !important;
    box-shadow: inset -3px 0 8px rgba(0,0,0,.5), 3px 0 12px rgba(0,0,0,.4);
}
[data-testid="stSidebar"]::before{
    content:"";
    position:absolute; inset:0;
    background: repeating-linear-gradient(
        0deg,
        transparent 0px,
        transparent 38px,
        rgba(139,69,19,.08) 38px,
        rgba(139,69,19,.15) 40px
    );
    pointer-events:none;
}
[data-testid="stSidebar"] *{
    font-family: 'Pixelify Sans', 'Inter', sans-serif !important;
    color: var(--text-dim) !important;
}
[data-testid="stSidebarNav"] a[aria-current="page"]{
    color: var(--text-gold) !important;
    background: var(--wood-surface) !important;
    border-radius: 6px;
    border-left: 3px solid var(--amber-gold) !important;
    box-shadow: inset 0 1px 0 var(--bevel-light), inset 0 -1px 0 var(--bevel-dark);
}

/* ───────── TYPOGRAPHY ───────── */
body, p, span, div, label{
    font-family: 'Inter', sans-serif;
    color: var(--text-cream);
}
h1, h2, h3, h4{
    font-family: 'Pixelify Sans', sans-serif !important;
    color: var(--text-gold) !important;
    text-shadow: 2px 2px 0 rgba(0,0,0,.5);
}
.block-container{
    padding-top: 2.5rem;
    padding-bottom: 4rem;
    max-width: 1100px;
}

/* ───────── EYEBROW / LABEL ───────── */
.aisle-eyebrow{
    font-family: 'VT323', monospace;
    font-size: 1rem;
    letter-spacing: .12em;
    text-transform: uppercase;
    color: var(--amber-gold);
    margin-bottom: 14px;
    display: flex;
    align-items: center;
    gap: 10px;
}
.aisle-eyebrow::before{
    content: "▸";
    color: var(--green-pixel);
    font-size: 1.1rem;
}

/* ───────── HERO SECTION ───────── */
.aisle-hero{
    background: var(--wood-panel);
    border: 3px solid var(--border-wood);
    border-radius: 8px;
    padding: 28px 32px;
    margin-bottom: 20px;
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        inset 2px 0 0 var(--bevel-light),
        inset -2px 0 0 var(--bevel-dark),
        0 8px 24px rgba(0,0,0,.45);
    position: relative;
}
.aisle-hero::before{
    content: "";
    position: absolute;
    inset: 4px;
    border: 1px solid rgba(200,132,74,.15);
    border-radius: 4px;
    pointer-events: none;
}
.aisle-hero h1{
    font-family: 'Pixelify Sans', sans-serif;
    font-weight: 700;
    font-size: clamp(1.9rem, 4vw, 2.8rem);
    line-height: 1.2;
    color: var(--text-cream);
    text-shadow: 2px 3px 0 rgba(0,0,0,.55);
    margin: 0 0 14px 0;
    max-width: 760px;
}
.aisle-hero h1 span{ color: var(--text-gold); }
.aisle-hero .lead{
    color: var(--text-dim);
    font-size: 1rem;
    max-width: 560px;
    margin-bottom: 0;
    line-height: 1.6;
}

/* ───────── STATS BAR — Thẻ chỉ số ───────── */
.aisle-stats{
    display: flex;
    gap: 0;
    border: 3px solid var(--border-wood);
    border-radius: 8px;
    overflow: hidden;
    margin-bottom: 12px;
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        0 6px 16px rgba(0,0,0,.35);
}
.aisle-stats .stat{
    background: var(--wood-surface);
    padding: 16px 22px;
    flex: 1;
    border-right: 2px solid var(--groove);
    position: relative;
}
.aisle-stats .stat:last-child{ border-right: none; }
.aisle-stats .stat::after{
    content: "";
    position: absolute;
    right: -1px;
    top: 8px;
    bottom: 8px;
    width: 1px;
    background: var(--bevel-light);
}
.aisle-stats .stat:last-child::after{ display: none; }
.aisle-stats .num{
    font-family: 'VT323', monospace;
    font-size: 1.6rem;
    color: var(--amber-gold);
    font-weight: 600;
    text-shadow: 1px 1px 0 rgba(0,0,0,.5);
}
.aisle-stats .label{
    color: var(--text-dim);
    font-size: .78rem;
    margin-top: 4px;
    font-family: 'Pixelify Sans', sans-serif;
}

/* ───────── STATUS PILLS ───────── */
.status-pill{
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: 'VT323', monospace;
    font-size: .85rem;
    padding: 3px 12px;
    border-radius: 4px;
}
.status-done{
    color: var(--green-pixel);
    border: 2px solid rgba(93,186,79,.4);
    background: rgba(93,186,79,.08);
}
.status-done .dot{
    width: 6px; height: 6px;
    border-radius: 50%;
    background: var(--green-pixel);
    animation: pixel-blink 1.2s steps(2) infinite;
}
.status-pending{
    color: var(--text-dim);
    border: 2px solid var(--groove);
    background: rgba(0,0,0,.15);
}

@keyframes pixel-blink{
    0%, 100%{ opacity:1; }
    50%{ opacity:0; }
}

/* ───────── NAV CARDS — Bảng gỗ treo ───────── */
[class*="st-key-navcard"]{
    background: var(--wood-panel) !important;
    border: 3px solid var(--border-wood) !important;
    border-radius: 8px !important;
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        0 6px 18px rgba(0,0,0,.35) !important;
    transition: transform .15s ease, box-shadow .15s ease, border-color .15s ease;
}
[class*="st-key-navcard"]:hover{
    transform: translateY(-4px);
    border-color: var(--amber-gold) !important;
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        0 12px 32px rgba(255,194,75,.15),
        0 0 20px rgba(255,194,75,.08) !important;
}
.card-idx{
    font-family: 'VT323', monospace;
    color: var(--amber-gold);
    font-size: 1rem;
    margin-bottom: 6px;
    text-shadow: 1px 1px 0 rgba(0,0,0,.4);
}
.card-title{
    font-family: 'Pixelify Sans', sans-serif;
    font-weight: 600;
    font-size: 1.05rem;
    color: var(--text-cream);
    margin: 2px 0 8px;
    text-shadow: 1px 1px 0 rgba(0,0,0,.35);
}
.card-desc{
    color: var(--text-dim);
    font-size: .85rem;
    line-height: 1.45;
    min-height: 2.6em;
}

/* ───────── PAGE LINKS ───────── */
[data-testid="stPageLink"]{ margin-top:4px; }
[data-testid="stPageLink"] p{
    font-family: 'VT323', monospace !important;
    font-size: 1rem !important;
    color: var(--green-pixel) !important;
    font-weight: 500 !important;
    text-shadow: 1px 1px 0 rgba(0,0,0,.3);
}
[data-testid="stPageLink"]:hover p{
    color: var(--text-gold) !important;
}

/* ───────── STREAMLIT BUTTONS — Nút gỗ ───────── */
[data-testid="stButton"] > button,
[data-testid="stFormSubmitButton"] > button{
    font-family: 'Pixelify Sans', sans-serif !important;
    background: linear-gradient(180deg, #8d4a23 0%, #6b3519 100%) !important;
    color: var(--text-cream) !important;
    border: 3px solid var(--border-wood) !important;
    border-radius: 6px !important;
    padding: 8px 20px !important;
    text-shadow: 1px 1px 0 rgba(0,0,0,.4);
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        0 3px 8px rgba(0,0,0,.3) !important;
    transition: all .12s ease;
}
[data-testid="stButton"] > button:hover,
[data-testid="stFormSubmitButton"] > button:hover{
    background: linear-gradient(180deg, #a86732 0%, #8d4a23 100%) !important;
    border-color: var(--amber-gold) !important;
    transform: translateY(-1px);
    box-shadow:
        inset 0 2px 0 var(--bevel-light),
        inset 0 -2px 0 var(--bevel-dark),
        0 6px 16px rgba(255,194,75,.2) !important;
}
[data-testid="stButton"] > button:active,
[data-testid="stFormSubmitButton"] > button:active{
    transform: translateY(1px);
    box-shadow:
        inset 0 -2px 0 var(--bevel-light),
        inset 0 2px 0 var(--bevel-dark) !important;
}

/* ───────── INPUTS — Ô nhập liệu gỗ ───────── */
[data-testid="stTextInput"] input,
[data-testid="stNumberInput"] input,
[data-testid="stTextArea"] textarea,
[data-testid="stSelectbox"] [data-baseweb="select"],
[data-testid="stMultiSelect"] [data-baseweb="select"]{
    background: var(--wood-darkest) !important;
    border: 2px solid var(--border-wood) !important;
    border-radius: 6px !important;
    color: var(--text-cream) !important;
    font-family: 'Inter', sans-serif !important;
    box-shadow: inset 0 2px 4px rgba(0,0,0,.3);
}
[data-testid="stTextInput"] input:focus,
[data-testid="stNumberInput"] input:focus,
[data-testid="stTextArea"] textarea:focus{
    border-color: var(--amber-gold) !important;
    box-shadow: inset 0 2px 4px rgba(0,0,0,.3), 0 0 8px rgba(255,194,75,.15) !important;
}

/* ───────── SLIDERS — Thanh trượt gỗ ───────── */
[data-testid="stSlider"] [role="slider"]{
    background: var(--amber-gold) !important;
    border: 2px solid var(--border-wood) !important;
    box-shadow: 0 2px 4px rgba(0,0,0,.4);
}
[data-testid="stSlider"] [data-testid="stThumbValue"]{
    font-family: 'VT323', monospace !important;
    color: var(--text-gold) !important;
}

/* ───────── METRICS — Thẻ thống kê ───────── */
[data-testid="stMetric"]{
    background: var(--wood-surface);
    border: 2px solid var(--border-wood);
    border-radius: 8px;
    padding: 14px 18px;
    box-shadow:
        inset 0 1px 0 var(--bevel-light),
        inset 0 -1px 0 var(--bevel-dark),
        0 4px 10px rgba(0,0,0,.25);
}
[data-testid="stMetric"] [data-testid="stMetricLabel"]{
    font-family: 'Pixelify Sans', sans-serif !important;
    color: var(--text-dim) !important;
}
[data-testid="stMetric"] [data-testid="stMetricValue"]{
    font-family: 'VT323', monospace !important;
    color: var(--amber-gold) !important;
    font-size: 1.8rem !important;
    text-shadow: 1px 1px 0 rgba(0,0,0,.4);
}
[data-testid="stMetric"] [data-testid="stMetricDelta"] svg{ display:none; }
[data-testid="stMetric"] [data-testid="stMetricDelta"]{
    font-family: 'VT323', monospace !important;
}

/* ───────── TABS — Tab gỗ ───────── */
[data-testid="stTabs"] [role="tab"]{
    font-family: 'Pixelify Sans', sans-serif !important;
    color: var(--text-dim) !important;
    border-bottom: 2px solid transparent;
    transition: all .12s ease;
}
[data-testid="stTabs"] [role="tab"][aria-selected="true"]{
    color: var(--text-gold) !important;
    border-bottom-color: var(--amber-gold) !important;
}
[data-testid="stTabs"] [data-baseweb="tab-highlight"]{
    background-color: var(--amber-gold) !important;
}

/* ───────── EXPANDER — Hộp mở rộng gỗ ───────── */
[data-testid="stExpander"]{
    background: var(--wood-panel) !important;
    border: 2px solid var(--border-wood) !important;
    border-radius: 8px !important;
    box-shadow: inset 0 1px 0 var(--bevel-light), inset 0 -1px 0 var(--bevel-dark);
}
[data-testid="stExpander"] summary{
    font-family: 'Pixelify Sans', sans-serif !important;
    color: var(--text-cream) !important;
}

/* ───────── DATAFRAME / TABLE — Bảng dữ liệu ───────── */
[data-testid="stDataFrame"]{
    border: 2px solid var(--border-wood) !important;
    border-radius: 6px !important;
    overflow: hidden;
}

/* ───────── FOOTER ───────── */
.aisle-footer{
    border-top: 2px solid var(--border-wood);
    padding-top: 16px;
    margin-top: 8px;
    font-family: 'VT323', monospace;
    font-size: 1rem;
    color: var(--text-dim);
    text-shadow: 1px 1px 0 rgba(0,0,0,.3);
}

/* ───────── REDUCED MOTION ───────── */
@media (prefers-reduced-motion: reduce){
    [class*="st-key-navcard"], .status-done .dot {
        animation: none !important;
        transition: none !important;
    }
}
</style>
"""


def apply_theme() -> None:
    st.markdown(CSS, unsafe_allow_html=True)
