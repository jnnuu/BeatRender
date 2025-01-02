# BeatRender

**BeatRender** is an open-source project that automates the process of visualizing audio tracks, creating stunning visualizer videos, and uploading them to YouTube. Designed to integrate seamlessly with **Azure Functions**, it provides a cloud-based, scalable solution for audio visualization and video publishing.

---

## Key Features

- **🎵 Audio Metadata Extraction**: Automatically extracts artist, title, genre, and more from MP3 files using `TagLib`.
- **🎨 Customizable Visualizers**: Generates video visualizations using `FFmpeg` with options for spectrum analysis, color customization, and text overlays.
- **☁️ Cloud Automation**: Leverages **Azure Blob Storage** and **Azure Functions** to process and visualize audio files automatically.
- **📤 YouTube Integration**: Uploads generated videos directly to YouTube using the **YouTube Data API v3**.
- **⚙️ Scalable Design**: Built for scalability and automation, making it ideal for batch audio-to-video rendering.

---

## How It Works

1. **Audio Input**:
   - Upload an MP3 file to an Azure Blob Storage container.
2. **Processing**:
   - Azure Function extracts metadata and processes the audio with `FFmpeg` to create a visualizer video.
3. **Output**:
   - The visualized video is saved to an Azure Blob Storage output container.
4. **YouTube Upload**:
   - The generated video is automatically uploaded to a specified YouTube channel.

---

## Technologies Used

- **Azure Functions**: For event-driven audio processing and video rendering.
- **Azure Blob Storage**: For input and output file storage.
- **FFmpeg**: For audio visualization and video generation.
- **YouTube Data API v3**: For automating YouTube uploads.
- **TagLib**: For extracting MP3 metadata.

---

## Use Cases

- DJs and music producers who want to quickly create and upload visualized mixes to YouTube.
- Automating batch video creation for music playlists.
- Creating engaging audio visualizations for podcasts, music, or soundscapes.

---

## Installation and Setup

### Prerequisites

- **Azure Account**: Set up an Azure subscription.
- **YouTube Data API Credentials**: Enable the YouTube Data API v3 and generate credentials.
- **FFmpeg**: Install FFmpeg on your development machine or ensure it is accessible in the Azure Function runtime.

### Steps

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/BeatRender.git
   cd BeatRender
   ```

2. **Set Up Azure Resources**:
   - Create two Azure Blob Storage containers: `beatrender-input` and `beatrender-output`.
   - Deploy Azure Functions to process the files.

3. **Configure Environment Variables**:
   - Add your YouTube API credentials and Azure Blob Storage connection strings to the function's app settings or `local.settings.json`.

4. **Deploy to Azure**:
   - Use the Azure CLI or Visual Studio Code to deploy the Azure Function.

---

## Contributing

Contributions are welcome! To contribute:

1. **Fork the Repository**:
   ```bash
   git fork https://github.com/yourusername/BeatRender.git
   ```

2. **Create a Feature Branch**:
   ```bash
   git checkout -b feature-name
   ```

3. **Submit a Pull Request**:
   Open a pull request with a clear description of your changes.

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---