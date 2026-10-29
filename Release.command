#!/bin/bash
# Çift tıkla: Doner Master Android release (doner.sh) bu klasörde çalışır.
cd "$(dirname "$0")" || exit 1
bash ./doner.sh "$@"
echo
read -r -p "Kapatmak için Enter'a bas..." _
