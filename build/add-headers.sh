#!/bin/bash
set -e pipefail

script_path=$(dirname $(realpath -s $0))/../

(find "$script_path" -name "*.fs" | grep -v "/bin/" | grep -v "/obj/")|while read fname; do
    bomb=$(hexdump -n 3 -C  "$fname" | head -n 1 | sed -e 's/[[:space:]]*//g' | cut -c 8-14 )
    line=$(head -n 1 "$fname")
    if [[ "$line" =~ ^/ ]] || [[ "$line" =~ ^# ]] ; then 
      echo "Skipped already starts with / or #: $fname"
    else 
      if [[ "$bomb" == "0efbbbf" ]]; then
        printf '\xEF\xBB\xBF' > "${fname}.new"
      fi
      sed -i $'1s/^\uFEFF//' $fname
      cat "${script_path}build/file-header.txt" "$fname" >> "${fname}.new"
      mv "${fname}.new" "$fname"
    fi
done

(find "$script_path" -name "*.cs" | grep -v "/bin/" | grep -v "/obj/")|while read fname; do
    bomb=$(hexdump -n 3 -C  "$fname" | head -n 1 | sed -e 's/[[:space:]]*//g' | cut -c 8-14 )
    line=$(head -n 1 "$fname")
    if [[ "$line" =~ ^/ ]] || [[ "$line" =~ ^# ]] ; then 
      echo "Skipped already starts with / or #: $fname"
    else 
      if [[ "$bomb" == "0efbbbf" ]]; then
        printf '\xEF\xBB\xBF' > "${fname}.new"
      fi
      sed -i $'1s/^\uFEFF//' $fname
      cat "${script_path}build/file-header.txt" "$fname" >> "${fname}.new"
      mv "${fname}.new" "$fname"
    fi
done
