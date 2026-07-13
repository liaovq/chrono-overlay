import { useEffect, useMemo, useState } from "react";
import {
  CheckCircle,
  DownloadSimple,
  GithubLogo,
  LockKey,
  ShieldCheck,
  Sparkle,
  Tray,
} from "@phosphor-icons/react";
import heroBackground from "./assets/hero-desktop-context.png";
import hueSpectrum from "./assets/hue-spectrum.png";

const repositoryUrl = "https://github.com/liaovq/chrono-overlay";
const desktopRuntimeUrl = "https://dotnet.microsoft.com/download/dotnet/8.0";

const weekdays = ["星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六"];

function formatClock(now) {
  const time = new Intl.DateTimeFormat("zh-CN", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  }).format(now);

  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return { time, date: `${year}.${month}.${day} ${weekdays[now.getDay()]}` };
}

function useClock() {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const delay = 1000 - (Date.now() % 1000);
    let interval;
    const timeout = window.setTimeout(() => {
      setNow(new Date());
      interval = window.setInterval(() => setNow(new Date()), 1000);
    }, delay);

    return () => {
      window.clearTimeout(timeout);
      window.clearInterval(interval);
    };
  }, []);

  return formatClock(now);
}

function Clock({ compact = false, color = "#f7f8fa", timeSize = 72, dateSize = 26 }) {
  const { time, date } = useClock();
  const styles = {
    "--clock-color": color,
    "--clock-time-size": `${compact ? Math.min(timeSize, 46) : timeSize}px`,
    "--clock-date-size": `${compact ? Math.min(dateSize, 17) : dateSize}px`,
  };

  return (
    <div className={`clock ${compact ? "clock--compact" : ""}`} style={styles} aria-label={`${time}，${date}`}>
      <time className="clock__time">{time}</time>
      <span className="clock__date">{date}</span>
    </div>
  );
}

function RangeControl({ label, value, min, max, suffix, onChange }) {
  return (
    <label className="range-control">
      <span>{label}</span>
      <input
        type="range"
        min={min}
        max={max}
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
      />
      <output>{value}{suffix}</output>
    </label>
  );
}

function ControlPanel({ onLock, settings, setSettings }) {
  const hueColor = useMemo(() => `hsl(${settings.hue} 88% 60%)`, [settings.hue]);

  return (
    <div className="control-panel" aria-label="悬浮时钟设置演示">
      <RangeControl
        label="时间字号"
        min="32"
        max="128"
        value={settings.timeSize}
        suffix="px"
        onChange={(timeSize) => setSettings({ ...settings, timeSize })}
      />
      <RangeControl
        label="日期字号"
        min="16"
        max="64"
        value={settings.dateSize}
        suffix="px"
        onChange={(dateSize) => setSettings({ ...settings, dateSize })}
      />
      <RangeControl
        label="背景浓度"
        min="0"
        max="100"
        value={settings.opacity}
        suffix="%"
        onChange={(opacity) => setSettings({ ...settings, opacity })}
      />
      <div className="color-control">
        <span>文字颜色</span>
        <button type="button" onClick={() => setSettings({ ...settings, color: "#000000" })}>纯黑</button>
        <label className="hue-control" style={{ backgroundImage: `url(${hueSpectrum})` }}>
          <span className="sr-only">色相</span>
          <input
            type="range"
            min="0"
            max="360"
            value={settings.hue}
            onChange={(event) => {
              const hue = Number(event.target.value);
              setSettings({ ...settings, hue, color: `hsl(${hue} 88% 60%)` });
            }}
            aria-label="文字色相"
          />
          <i style={{ left: `${settings.hue / 3.6}%`, background: hueColor }} />
        </label>
        <button type="button" onClick={() => setSettings({ ...settings, color: "#ffffff" })}>纯白</button>
      </div>
      <div className="panel-footer">
        <label className="autostart"><input type="checkbox" /> 开机自启</label>
        <button className="lock-button" type="button" onClick={onLock}><LockKey size={17} weight="bold" />锁定</button>
      </div>
    </div>
  );
}

function DownloadButton({ className = "" }) {
  return (
    <a className={`button button--primary ${className}`} href={__DOWNLOAD_URL__}>
      <DownloadSimple size={22} weight="bold" />下载 Windows 版
    </a>
  );
}

export function App() {
  const [locked, setLocked] = useState(false);
  const [settings, setSettings] = useState({
    timeSize: 72,
    dateSize: 32,
    opacity: 60,
    hue: 220,
    color: "#ffffff",
  });

  return (
    <main>
      <header className="site-header">
        <a className="brand" href="#top" aria-label="ChronoOverlay 首页">ChronoOverlay</a>
        <nav aria-label="主要导航">
          <a href="#experience">产品体验</a>
          <a href="#privacy">隐私</a>
          <a href={repositoryUrl}>GitHub</a>
        </nav>
      </header>

      <section className="hero" id="top" style={{ backgroundImage: `url(${heroBackground})` }}>
        <div className="hero__content">
          <p className="eyebrow"><Sparkle size={16} weight="fill" /> 安静驻留在你的桌面</p>
          <h1>ChronoOverlay</h1>
          <p className="hero__summary">一个轻量、可锁定、始终置顶的 Windows 桌面悬浮时钟。</p>
          <div className="hero__actions">
            <DownloadButton />
            <a className="button button--secondary" href={repositoryUrl}>
              <GithubLogo size={22} weight="fill" />查看 GitHub
            </a>
          </div>
          <div className="meta-row">
            <span>v{__APP_VERSION__}</span>
            <span>Windows 10/11 x64</span>
            <span>需 .NET 8 Desktop Runtime</span>
            <span>MIT 开源</span>
          </div>
        </div>
        <div className="hero__clock" aria-label="网页实时演示">
          <Clock />
          <span>网页实时演示</span>
        </div>
      </section>

      <section className="experience" id="experience">
        <div className="section-heading">
          <p className="eyebrow">专注时间本身</p>
          <h2>为专注而生，轻盈不打扰</h2>
          <p>调整好样式与位置后锁定，让时间留在视线里，而不是挡在工作前。</p>
        </div>

        <div className="state-showcase">
          <article className="state-column state-column--unlocked">
            <div className="state-heading">
              <span>01</span>
              <div>
                <h3>解锁状态 · 自由定制</h3>
                <p>字号、颜色、背景浓度和位置都能即时调整。</p>
              </div>
            </div>
            <div className="demo-stage">
              <div className="demo-clock" style={{ backgroundColor: `rgb(0 0 0 / ${settings.opacity}%)` }}>
                <Clock compact color={settings.color} timeSize={settings.timeSize} dateSize={settings.dateSize} />
              </div>
              {!locked && <ControlPanel onLock={() => setLocked(true)} settings={settings} setSettings={setSettings} />}
              {locked && (
                <button className="unlock-hint" type="button" onDoubleClick={() => setLocked(false)}>
                  已锁定。双击这里解锁演示
                </button>
              )}
            </div>
            <ul className="check-list">
              <li><CheckCircle weight="fill" />拖动到桌面的任意位置</li>
              <li><CheckCircle weight="fill" />分别调整时间与日期字号</li>
              <li><CheckCircle weight="fill" />颜色和背景浓度即时生效</li>
            </ul>
          </article>

          <article className="state-column state-column--locked">
            <div className="state-heading">
              <span>02</span>
              <div>
                <h3>锁定状态 · 专注不扰</h3>
                <p>位置固定、背景和日期区域点击穿透。</p>
              </div>
            </div>
            <div className="locked-stage">
              <Clock compact />
              <div className="locked-message">
                <LockKey size={38} weight="duotone" />
                <strong>已锁定并开启选择性穿透</strong>
                <span>只有时间数字区域会拦截点击，用于双击解锁。</span>
              </div>
            </div>
            <ul className="check-list">
              <li><CheckCircle weight="fill" />普通窗口上方始终可见</li>
              <li><CheckCircle weight="fill" />托盘随时恢复控制面板</li>
              <li><CheckCircle weight="fill" />下次启动完整恢复状态</li>
            </ul>
          </article>
        </div>

        <div className="privacy-note" id="privacy">
          <ShieldCheck size={22} weight="duotone" />
          <span><strong>只在本机运行。</strong>不联网、不收集数据、不加入遥测。</span>
        </div>
      </section>

      <section className="download-section">
        <div className="download-mark"><Tray size={42} weight="duotone" /></div>
        <p className="eyebrow">现在就让时间安静陪伴你</p>
        <h2>立即体验 <span>ChronoOverlay</span></h2>
        <p>轻量单文件 EXE，需要预先安装 x64 .NET 8 Desktop Runtime。</p>
        <DownloadButton className="download-section__button" />
        <div className="meta-row meta-row--center">
          <span>v{__APP_VERSION__}</span>
          <span>Windows 10/11 x64</span>
        </div>
        <a className="runtime-link" href={desktopRuntimeUrl}>下载微软 .NET 8 Desktop Runtime</a>
        <a className="github-link" href={repositoryUrl}><GithubLogo size={20} weight="fill" />查看 GitHub</a>
      </section>

      <footer>
        <span>ChronoOverlay · v{__APP_VERSION__}</span>
        <span>MIT License</span>
        <span>不收集数据</span>
      </footer>
    </main>
  );
}
