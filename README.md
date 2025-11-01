# cinemalist

## Local Development Configuration

For local development, you can create an `appsettings.override.json` file to override any configuration settings without affecting the repository.

### Setup
1. Create a file named `appsettings.override.json` in the root directory of the project.
2. Edit `appsettings.override.json` with your local settings:
   - Database connection strings
   - API keys
   - Other environment-specific settings

This file is automatically loaded last and will override all previous configuration sources. It's already included in `.gitignore` so your local settings won't be committed to the repository.

