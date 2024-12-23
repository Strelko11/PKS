#!/bin/bash


DEST_IP="127.0.0.1"
DEST_LISTENING_PORT=12347
DEST_SENDING_PORT=12348
SOURCE_LISTENING_PORT=12345
SOURCE_SENDING_PORT=12346
INITIATE="n"

dotnet build Computer1.csproj

dotnet run --project Computer1.csproj "$DEST_IP" "$DEST_LISTENING_PORT" "$DEST_SENDING_PORT" "$SOURCE_LISTENING_PORT" "$SOURCE_SENDING_PORT" "$INITIATE"
