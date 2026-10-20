# 天机阁 - 武将头像库 (Avatars)

## 说明
本目录存放三国名将头像图片资源（如 `guanyu.png`, `taishici.png`, `zhugeliang.png`, `lvbu.png` 等）。

- **格式支持**：`.png`, `.jpg`, `.jpeg`, `.bmp`, `.jfif`
- **尺寸推荐**：64x64 或 128x128 像素正方形或微圆角
- **配置方式**：在 `data/Generals.json` 中配置对应武将的 `"avatar"` 字段，例如：
  ```json
  "avatar": "avatars/guanyu.png"
  ```
- **智能降级回退**：若图片文件不存在，天机阁将自动降级展示典雅的“古风武将印玺”（提取武将姓氏单字朱砂金印），界面永不白屏报错。
