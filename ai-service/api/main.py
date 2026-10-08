from fastapi import FastAPI

app = FastAPI(title="AI-Supported Homework Platform - AI Service")

@app.get("/")
def read_root():
    return {"status": "ok", "message": "Python AI Service is running"}
