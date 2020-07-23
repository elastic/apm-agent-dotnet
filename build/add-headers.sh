#!/bin/bash
set -e pipefail

# anchor to the root
script_path=$(dirname $(realpath -s $0))/../

bomb_chars='\xEF\xBB\xBF'
file_header_present_re='^(/|#)'

addLicenseHeader() {
  (find "$script_path" -name $1 | grep -v "/bin/" | grep -v "/obj/")|while read fname; do
      bomb=$(hexdump -n 3 -C  "$fname" | head -n 1 | sed -e 's/[[:space:]]*//g' | cut -c 8-14 )
      line=$(head -n 1 "$fname" | sed -e "s/^\xef\xbb\xbf//")
      if [[ "$line" =~ $file_header_present_re ]]; then 
        echo "Skipped already starts with / or #: $fname"
      else 
        if [[ "$bomb" == "0efbbbf" ]]; then
          printf $bomb_chars > "${fname}.new"
        fi
        sed -i $'1s/^\uFEFF//' $fname
        cat "${script_path}build/file-header.txt" "$fname" >> "${fname}.new"
        mv "${fname}.new" "$fname"
      fi
  done
}

addLicenseHeader "*.cs"
addLicenseHeader "*.fs"
